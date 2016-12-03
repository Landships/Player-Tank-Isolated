using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System;


public class PlayerController_VR : MonoBehaviour
{
    public byte owner;
    byte current_player;

    public GameObject camera_rig;
    public GameObject left_controller;
    public GameObject right_controller;

    public GameObject left_hand;
    public GameObject right_hand;

    // Client Queue
    int frame = 0;
    Queue<Vector3> past_left_positions;
    Queue<Vector3> past_right_positions;

    // Lerping
    bool left_reconcile = false;
    bool right_reconcile = false;
    float lerp_time = 1.0f;
    float current_left_lerp_time;
    float current_right_lerp_time;
    Vector3 lerp_final_left_position;
    Vector3 lerp_final_right_position;

    //Client to send
    byte[] client_info = new byte[24];
    float[] client_cache = new float[6];


    int server_player;

    // general
    float left_x;
    float left_y;
    float left_z;

    float right_x;
    float right_y;
    float right_z;


    //trigger

    GameObject n_manager;
    network_manager n_manager_script;

    bool started = false;
    bool ready = false;

    bool reliable_message = false;


    void Start()
    {
        n_manager = GameObject.Find("Custom Network Manager(Clone)");
        n_manager_script = n_manager.GetComponent<network_manager>();
        current_player = (byte)(n_manager_script.client_players_amount);
        //client_update_values(n_manager_script.server_to_client_data_large);
        //server_get_values_to_send();

        past_left_positions = new Queue<Vector3>(10);
        past_right_positions = new Queue<Vector3>(10);
    }

    void Update()
    {
        started = n_manager_script.started;
        ready = n_manager_script.game_ready;

        server_player = n_manager_script.server_player_control;

        reliable_message = n_manager_script.reliable_message;

        if (current_player == 1)
        {

            //Debug.Log("job for the server");
            // Server Updates world based off a clients inputs
            if (server_player == owner)
            {
                if (reliable_message)
                {
                    Debug.Log("Server_recieved a reliable message");
                    left_hand.GetComponent<Renderer>().material.color = Color.green;
                }
                else
                {
                    server_update_values(n_manager_script.server_to_client_data);
                }
            }
            update_world_state();
            server_get_values_to_send();
        }

        else
        {
            if (owner == current_player)
            {
                if (frame == 10)
                {
                    frame = -1;
                    client_update_values();
                    client_reconciliation();
                }
                frame++;

                if (left_reconcile == true)
                {
                    reconcile_player_left_position();
                }
                if (right_reconcile == true)
                {
                    reconcile_player_right_position();
                }
            }
            else
            {
                 client_update_values();
            }
            update_world_state();
        }
    }

    public void server_update_values(byte[] client_inputs)
    {
        float[] back = new float[6];

        Buffer.BlockCopy(client_inputs, 0, back, 0, client_inputs.Length);

        left_x = back[0];
        left_y = back[1];
        left_z = back[2];

        Debug.Log("left controller vector3: " + right_x + " " + right_y + " " + right_z);

        right_x = back[3];
        right_y = back[4];
        right_z = back[5];
    }

    //if not owner and not host, do nothing, else:
    void update_world_state()
    {
        if (current_player == 1 && current_player == owner)
        {
            Read_Camera_Rig();
            //past_left_positions.Enqueue(left_hand.transform.position);
            //past_right_positions.Enqueue(right_hand.transform.position);
        }
        if (current_player == 1 && current_player != owner)
        {
            if (owner == 2)
                Debug.Log("Player 2");
            //past_left_positions.Enqueue(new Vector3(left_x, left_y, left_z));
            //past_right_positions.Enqueue(new Vector3(right_x, right_y, right_z));

            left_hand.transform.position = new Vector3(left_x, left_y, left_z);
            right_hand.transform.position = new Vector3(right_x, right_y, right_z);
        }
        if (current_player != 1 && current_player == owner)
        {
            Read_Camera_Rig();
            past_left_positions.Enqueue(left_controller.transform.position);
            past_right_positions.Enqueue(right_controller.transform.position);

            if (frame == 10)
            {
                client_send_values();
            }
            
        }
        if (current_player != 1 && current_player != owner)
        {
          
                //past_left_positions.Enqueue(new Vector3(left_x, left_y, left_z));
                //past_right_positions.Enqueue(new Vector3(right_x, right_y, right_z));

            left_hand.transform.position = new Vector3(left_x, left_y, left_z);
            right_hand.transform.position = new Vector3(right_x, right_y, right_z);
        }

    }

    void client_update_values()
    {

        //byte[] client_new_world = n_manager_script.server_to_client_data_large;
        float[] data = new float[24];
        Buffer.BlockCopy(n_manager_script.server_to_client_data_large, 3, data, 0, 96);

        // 6 * 4byte things
        // 4byte things = left_handx, left_handy, left_handz, right_handx, right_handy, right_handz 
        int offset = 6;
        int index = 0;
        if (owner == 2)
        {
            index = index + offset;
        }
        if (owner == 3)
        {
            index = index + offset + offset;
        }
        if (owner == 4)
        {
            index = index + offset + offset + offset;
        }


        left_x = data[index];
        left_y = data[index + 1];
        left_z = data[index + 2];
        right_x = data[index + 3];
        right_y = data[index + 4];
        right_z = data[index + 5];

    }

    void client_reconciliation()
    {
        // The client is going to make a decision whether the new x y z data it recieved from the server is one 
        // that it has seen before and if so keep on using client side inputs.
        // If it has never been in that position before then it must move back to that location


        bool left_found = false;
        while (past_left_positions.Count != 0 && left_found != true)
        {
            Vector3 left_past_position = past_left_positions.Dequeue();

            Vector3 server_left_postion = new Vector3(left_x, left_y, left_z);
            float server_left_sq_distance = Vector3.Distance(left_past_position, server_left_postion);
            if (server_left_sq_distance < .05)
            {

                left_found = true;
            }
        }

        bool right_found = false;
        while (past_right_positions.Count != 0 && right_found != true)
        {
            Vector3 right_past_position = past_right_positions.Dequeue();

            Vector3 server_right_postion = new Vector3(right_x, right_y, right_z);
            float server_right_sq_distance = Vector3.Distance(right_past_position, server_right_postion);
            if (server_right_sq_distance < .05)
            {

                right_found = true;
            }
        }

        if (left_found == false)
        {
            left_reconcile = true;
            lerp_final_left_position = new Vector3(left_x, left_y, left_z);
            current_left_lerp_time = 0f;
        }

        if (right_found == false)
        {
            right_reconcile = true;
            lerp_final_right_position = new Vector3(right_x, right_y, right_z);
            current_right_lerp_time = 0f;
        }
    }


    public void server_get_values_to_send()
    {

        float[] data_cache = new float[24];
        byte one = n_manager_script.server_to_client_data_large[0];
        byte two = n_manager_script.server_to_client_data_large[1];
        byte three = n_manager_script.server_to_client_data_large[2];

        Buffer.BlockCopy(n_manager_script.server_to_client_data_large, 3, data_cache, 0, 96);

        int offset = 6;
        int index = 0;
        if (owner == 2)
        {
            index = index + offset;
        }
        if (owner == 3)
        {
            index = index + offset + offset;
        }
        if (owner == 4)
        {
            index = index + offset + offset + offset;
        }

        data_cache[index] = left_hand.transform.position.x;
        data_cache[index + 1] = left_hand.transform.position.y;
        data_cache[index + 2] = left_hand.transform.position.z;
        data_cache[index + 3] = right_hand.transform.position.x;
        data_cache[index + 4] = right_hand.transform.position.y;
        data_cache[index + 5] = right_hand.transform.position.z;

        byte[] data_out = new byte[99];
        Buffer.BlockCopy(data_cache, 0, data_out, 3, 96);
        data_out[0] = one;
        data_out[1] = two;
        data_out[2] = three;

        //Buffer.BlockCopy(data_out, 0, n_manager_script.server_to_client_data_large, 0, 115);
        //Debug.Log("Server should be here");
        n_manager_script.server_to_client_data_large = data_out;






    }


    void client_send_values()
    {

        client_cache[0] = left_controller.transform.position.x;
        client_cache[1] = left_controller.transform.position.y;
        client_cache[2] = left_controller.transform.position.z;
        client_cache[3] = right_controller.transform.position.x;
        client_cache[4] = right_controller.transform.position.y;
        client_cache[5] = right_controller.transform.position.z;
        Buffer.BlockCopy(client_cache, 0, client_info, 0, 24);

        Debug.Log("Left controller sending: " + right_controller.transform.position.ToString());

        n_manager_script.client_send_information(client_info);

    }



    void Read_Camera_Rig()
    {
        left_hand.transform.position = left_controller.transform.position;
        right_hand.transform.position = right_controller.transform.position;


    }


    void reconcile_player_left_position()
    {
        current_left_lerp_time += Time.deltaTime;
        if (current_left_lerp_time > lerp_time)
        {
            left_reconcile = false;
            current_left_lerp_time = lerp_time;
        }
        float percent = current_left_lerp_time / lerp_time;
        left_hand.transform.position = Vector3.Lerp(left_hand.transform.position, lerp_final_left_position, percent);
    }

    void reconcile_player_right_position()
    {
        current_right_lerp_time += Time.deltaTime;
        if (current_right_lerp_time > lerp_time)
        {
            right_reconcile = false;
            current_right_lerp_time = lerp_time;
        }
        float percent = current_right_lerp_time / lerp_time;
        right_hand.transform.position = Vector3.Lerp(right_hand.transform.position, lerp_final_right_position, percent);
    }

    public byte get_client_player_number()
    {
        return current_player;
    }


    public void add_trigger_listener()
    {
        right_controller.GetComponent<VRTK.VRTK_ControllerEvents>().TriggerClicked += new VRTK.ControllerInteractionEventHandler(client_send_reliable_message);
    }

    void client_send_reliable_message(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        Debug.Log("CLICKED");
        if (current_player == 1)
        {
            n_manager_script.server_send_reliable();
        }
        else
        {
            n_manager_script.client_send_reliable();
        }
    }


}