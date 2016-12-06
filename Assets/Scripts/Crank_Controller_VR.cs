﻿using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System;

/// <summary>
/// THIS IS OWNED BY PLAYER 2
/// </summary>

public class Crank_Controller_VR : MonoBehaviour
{
    byte current_player; // owner = player 2

    public GameObject vertical_crank;
    public GameObject horizontal_crank;

    // Client Queue
    int frame = 0;

    int server_player;

    // general
    float vertical_x;

    float horizontal_x;


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
    }

    void Update()
    {
        started = n_manager_script.started;
        ready = n_manager_script.game_ready;

        server_player = n_manager_script.server_player_control;

        reliable_message = n_manager_script.reliable_message;

        if (current_player == 2)
        {
            client_send_values();
        }

        else
        {
            server_get_client_hands();
        }
    }

    void FixedUpdate()
    {
        update_world_state();
    }


    //if not owner and not host, do nothing, else:
    void update_world_state()
    {
        if (current_player == 2)
        {
            //
        }
        else
        {
            vertical_crank.transform.localRotation = Quaternion.Euler(vertical_x, 0, 0);
            horizontal_crank.transform.localRotation = Quaternion.Euler(horizontal_x, 0, 0);
        }
    }

    public byte get_client_player_number()
    {
        return current_player;
    }


    public void add_trigger_listener()
    {
        //right_controller.GetComponent<VRTK.VRTK_ControllerEvents>().TriggerClicked += new VRTK.ControllerInteractionEventHandler(client_send_reliable_message);
    }


    // ----------------------------
    // Functions that use Block Copy
    // ----------------------------

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


    // The client get its values/inputs to send to the server
    void client_send_values()
    {
        float[] vertical_crank_values = { vertical_crank.transform.localRotation.eulerAngles.x };
        float[] horizontal_crank_values = { horizontal_crank.transform.localRotation.eulerAngles.x };

        n_manager_script.send_from_client(10, vertical_crank_values);
        n_manager_script.send_from_client(11, horizontal_crank_values);

    }



    // Server Updates the server larger buffer it is going to send
    public void server_get_values_to_send()
    {

        float[] vertical_crank_values = { vertical_crank.transform.localRotation.eulerAngles.x };
        float[] horizontal_crank_values = { horizontal_crank.transform.localRotation.eulerAngles.x };



        n_manager_script.send_from_server(10, vertical_crank_values);
        n_manager_script.send_from_server(11, horizontal_crank_values);

    }




    // Client get values from the server buffer
    void client_update_values()
    {

        float[] vertical_crank_values = n_manager_script.client_read_server_buffer(10);
        float[] horizontal_crank_values = n_manager_script.client_read_server_buffer(11);

        vertical_x = vertical_crank_values[0];

        horizontal_x = horizontal_crank_values[0];


    }

    // Server Get values from the client buffer, so the client inputs
    public void server_get_client_hands()
    {
        float[] vertical_crank_values = n_manager_script.server_read_client_buffer(10);
        float[] horizontal_crank_values = n_manager_script.server_read_client_buffer(11);

        vertical_x = vertical_crank_values[0];

        horizontal_x = horizontal_crank_values[0];

    }
}