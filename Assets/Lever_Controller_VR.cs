using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System;

/// <summary>
/// THIS IS OWNED BY PLAYER 1/SERVER
/// </summary>

public class Lever_Controller_VR : MonoBehaviour
{
    public bool owner; // owner = server/player 1

    public GameObject left_lever;
    public GameObject right_lever;

    // Client Queue
    int frame = 0;

    //Client to send
    byte[] client_info = new byte[24];
    float[] client_cache = new float[6];


    int server_player;

    // general
    float leftr_x;
    float leftr_y;
    float leftr_z;

    float rightr_x;
    float rightr_y;
    float rightr_z;


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
    }

    void Update()
    {
        started = n_manager_script.started;
        ready = n_manager_script.game_ready;

        server_player = n_manager_script.server_player_control;

        reliable_message = n_manager_script.reliable_message;

        if (owner)
        {
            //send to clients
            server_get_values_to_send();
        }
        else
        {
            //receive from server
            client_update_values();
        }
    }

    void FixedUpdate()
    {
        update_world_state();
    }

    public void server_read_buffer(byte[] client_inputs)
    {
        float[] back = new float[6];

        Buffer.BlockCopy(client_inputs, 0, back, 0, client_inputs.Length);

        leftr_x = back[0];
        leftr_y = back[1];
        leftr_z = back[2];

        rightr_x = back[3];
        rightr_y = back[4];
        rightr_z = back[5];
    }

    //if not owner and not host, do nothing, else:
    void update_world_state()
    {
        if (owner)
        {
            //nothing
        }
        else
        {
           left_lever.transform.localRotation = Quaternion.Euler(leftr_x, leftr_y, leftr_z);
           right_lever.transform.localRotation = Quaternion.Euler(rightr_x, rightr_y, rightr_z);
        }
    }

    void client_update_values()
    {

        //byte[] client_new_world = n_manager_script.server_to_client_data_large;
        float[] data = new float[24];
        Buffer.BlockCopy(n_manager_script.server_to_client_data_large, 3, data, 0, 96);

        // 6 * 4byte things
        // 4byte things = left_handx, left_handy, left_handz, right_handx, right_handy, right_handz 
        int index = 0;
  
        leftr_x = data[index];
        leftr_y = data[index + 1];
        leftr_z = data[index + 2];
        rightr_x = data[index + 3];
        rightr_y = data[index + 4];
        rightr_z = data[index + 5];

    }

   
    public void server_get_values_to_send()
    {

        float[] data_cache = new float[24];
        byte one = n_manager_script.server_to_client_data_large[0];
        byte two = n_manager_script.server_to_client_data_large[1];
        byte three = n_manager_script.server_to_client_data_large[2];

        Buffer.BlockCopy(n_manager_script.server_to_client_data_large, 3, data_cache, 0, 96);

        int index = 0;

        data_cache[index] = left_lever.transform.localRotation.x;
        data_cache[index + 1] = left_lever.transform.localRotation.y;
        data_cache[index + 2] = left_lever.transform.localRotation.z;
        data_cache[index + 3] = right_lever.transform.localRotation.x;
        data_cache[index + 4] = right_lever.transform.localRotation.y;
        data_cache[index + 5] = right_lever.transform.localRotation.z;

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

        client_cache[0] = left_lever.transform.localRotation.x;
        client_cache[1] = left_lever.transform.localRotation.y;
        client_cache[2] = left_lever.transform.localRotation.z;
        client_cache[3] = right_lever.transform.localRotation.x;
        client_cache[4] = right_lever.transform.localRotation.y;
        client_cache[5] = right_lever.transform.localRotation.z;

        //Buffer.BlockCopy(client_cache, 0, n_manager_script.client_info, 0, 24);

        n_manager_script.client_send_information();

    }

    public void add_trigger_listener()
    {
        //right_controller.GetComponent<VRTK.VRTK_ControllerEvents>().TriggerClicked += new VRTK.ControllerInteractionEventHandler(client_send_reliable_message);
    }

    void client_send_reliable_message(object sender, VRTK.ControllerInteractionEventArgs e)
    {
        Debug.Log("CLICKED");
    }


}