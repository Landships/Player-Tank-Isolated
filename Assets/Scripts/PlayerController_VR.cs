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
    byte[] client_info = new byte[100];
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

    int frame_interval = 5;


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
                    server_get_client_hands();
                }
            }
            update_world_state();
            server_get_values_to_send();
        }

        else
        {
            if (owner == current_player)
            {
                if (frame == frame_interval)
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

            if (frame == frame_interval)
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












    // ----------------------------
    // Functions that use Block Copy
    // ----------------------------

    void client_send_reliable_message(object sender, VRTK.ControllerInteractionEventArgs e) {
        Debug.Log("CLICKED");
        if (current_player == 1) {
            n_manager_script.server_send_reliable();
        } else {
            n_manager_script.client_send_reliable();
        }
    }


    // The client get its values/inputs to send to the server
    void client_send_values() {
        float[] left_controller_values = { left_controller.transform.position.x,
                                           left_controller.transform.position.y,
                                           left_controller.transform.position.z };
        float[] right_controller_values = { right_controller.transform.position.x,
                                            right_controller.transform.position.y,
                                            right_controller.transform.position.z };

        n_manager_script.send_from_client(1, left_controller_values);
        n_manager_script.send_from_client(2, right_controller_values);

    }



    // Server Updates the server larger buffer it is going to send
    public void server_get_values_to_send() {

        float[] left_controller_values = { left_hand.transform.position.x,
                                           left_hand.transform.position.y,
                                           left_hand.transform.position.z };
        float[] right_controller_values = { right_hand.transform.position.x,
                                            right_hand.transform.position.y,
                                            right_hand.transform.position.z };



        n_manager_script.send_from_server(1, left_controller_values);
        n_manager_script.send_from_server(2, right_controller_values);

    }




    // Client get values from the server buffer
    void client_update_values() {

        float[] left_controller_values = n_manager_script.client_read_server_buffer(1);
        float[] right_controller_values = n_manager_script.client_read_server_buffer(2);

        left_x = left_controller_values[0];
        left_y = left_controller_values[1];
        left_z = left_controller_values[2];

        right_x = right_controller_values[0];
        right_y = right_controller_values[1];
        right_z = right_controller_values[2];

    }

        // Server Get values from the client buffer, so the client inputs
    public void server_get_client_hands()
    {
        float[] left_controller_values = n_manager_script.server_read_client_buffer(1);
        float[] right_controller_values = n_manager_script.server_read_client_buffer(2);

        left_x = left_controller_values[0];
        left_y = left_controller_values[1];
        left_z = left_controller_values[2];

        Debug.Log("left controller vector3: " + right_x + " " + right_y + " " + right_z);

        right_x = right_controller_values[0];
        right_y = right_controller_values[1];
        right_z = right_controller_values[2];
    }





}