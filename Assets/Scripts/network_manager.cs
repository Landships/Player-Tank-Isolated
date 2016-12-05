using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

public class network_manager : MonoBehaviour
{

    static bool is_host;
    Canvas_Manager manager_script;
    GameObject server_lobby;
    GameObject join_lobby;
    int frame = 0;

    //Network variables
    string server_ip;
    public bool game_ready = false;
    static byte server_players_amount = 0;

    //Server Stuff
    int server_port = 8888;
    int server_reliable_channel;
    int server_real_reliable_channel;
    static int[] server_client_connection = new int[4];
    static int server_socket_ID;
    int max_connections = 10;

    public byte[] server_to_client_data = new byte[24];
    public int server_player_control = -1;

    public byte[] server_reliable_buffer = new byte[99];

    //Client stuff
    int client_socket_ID;
    int client_reliable_channel;
    int client_connection;
    bool client_joined = false;
    public byte client_players_amount = 0;
    public byte[] client_reliable_buffer = new byte[24];


    public byte[] server_to_client_data_large = new byte[99];

    public bool started = false;

    public bool reliable_message = false;

    public byte[] client_info = new byte[240] 


    void Start()
    {

        //Array.Clear(server_to_client_data, 0, 24);

        GameObject custom_network_manager = GameObject.Find("Game Manager(Clone)");
        manager_script = custom_network_manager.GetComponent<Canvas_Manager>();
        is_host = manager_script.get_host_status();
        Debug.Log(manager_script.get_host_status().ToString());
        //server_ip = manager_script.get_address();
        server_ip = manager_script.get_inserted_ip();

        if (is_host)
        {
            server_client_connection[server_players_amount] = 0;
            server_players_amount++;
            client_players_amount++;

            join("Player 1");
            server_setup();

        }

        if (!is_host && server_ip != "")
        {
            client_setup();
            connect_to_server(server_ip);

        }

    }


    void Update()
    {


        /// Game Not running ///
        if (!game_ready && is_host)
        {
            //Debug.Log("Server is going to the right place"); ;
            server_game_not_ready();
        }
        if (!game_ready && !is_host)
        {
            client_lobby_update();

        }
        /// Game Not running ///



        if (client_joined && game_ready)
        {
            //do game stuff Client
            if (started)
            {
                client_get_large_message_from_server();
            }
        }

        if (is_host && game_ready)
        {
            //do Game Stuff SERVER

            if (started == true)
            {

                if (frame == 0)
                {
                    server_send_large_message_to_client();
                    frame = 1;
                }
                else
                {
                    frame = 0;
                }
                server_get_client_player_data();

                //client_get_data_to_send()

                //server_send_large_message_to_client();
            }


            if (!started)
            {
                tell_clients_to_start();
                started = true;
            }



        }


    }





    void join(string player_update)
    {
        server_lobby = GameObject.Find("Server Lobby(Clone)");
        GameObject player = server_lobby.transform.Find(player_update).gameObject;
        GameObject player_status = player.transform.Find("Player Status").gameObject;

        Text status = player_status.GetComponent<Text>();
        status.text = player_update + "\nIn Lobby";
        status.color = new Color(0, 255, 0);

        RawImage image = player.GetComponent<RawImage>();
        image.color = new Color(0, 255, 0);
    }


    void server_setup()
    {
        /// Global Config defines global paramters for network library.
        GlobalConfig global_configuration = new GlobalConfig();
        global_configuration.ReactorModel = ReactorModel.SelectReactor;
        global_configuration.ThreadAwakeTimeout = 10;

        /// Add a channel to send and recieve 
        /// Build channel configuration
        ConnectionConfig connection_configuration = new ConnectionConfig();
        server_real_reliable_channel = connection_configuration.AddChannel(QosType.Reliable);
        server_reliable_channel = connection_configuration.AddChannel(QosType.UnreliableSequenced);



        /// Create Network Topology for host configuration
        /// This topology defines: 
        /// (1) how many connection with default config will be supported/
        /// (2) what will be special connections (connections with config different from default).
        HostTopology host_topology = new HostTopology(connection_configuration, max_connections);

        /// Initializes the NetworkTransport. 
        /// Should be called before any other operations on the NetworkTransport are done.
        NetworkTransport.Init();

        // Open sockets for server and client
        server_socket_ID = NetworkTransport.AddHost(host_topology, server_port);
        if (server_socket_ID < 0) { Debug.Log("Server socket creation failed!"); } else { Debug.Log("Server socket creation successful!"); }

    }


    void client_setup()
    {
        /// Global Config defines global paramters for network library.
        GlobalConfig global_configuration = new GlobalConfig();
        global_configuration.ReactorModel = ReactorModel.SelectReactor;
        global_configuration.ThreadAwakeTimeout = 10;

        /// Add a channel to send and recieve 
        /// Build channel configuration
        ConnectionConfig connection_configuration = new ConnectionConfig();
        server_real_reliable_channel = connection_configuration.AddChannel(QosType.Reliable);
        client_reliable_channel = connection_configuration.AddChannel(QosType.UnreliableSequenced);


        /// Create Network Topology for host configuration
        /// This topology defines: 
        /// (1) how many connection with default config will be supported/
        /// (2) what will be special connections (connections with config different from default).
        HostTopology host_topology = new HostTopology(connection_configuration, 5);

        /// Initializes the NetworkTransport. 
        /// Should be called before any other operations on the NetworkTransport are done.
        NetworkTransport.Init();

        // Open sockets for server and client
        client_socket_ID = NetworkTransport.AddHost(host_topology);
        if (client_socket_ID < 0) { Debug.Log("Client socket creation failed!"); } else { Debug.Log("Client socket creation successful!"); }

    }




    void connect_to_server(string ip)
    {
        byte error;
        client_connection = NetworkTransport.Connect(client_socket_ID, ip, server_port, 0, out error);
        if (error != 0)
        {
            Debug.Log("I FAILED to send my request to connect to the server");
            Debug.Log(error.ToString());
        }
        else
        {
            Debug.Log("I Sent my request to connect");
        }
    }








    void server_game_not_ready()
    {
        byte error;
        //Debug.Log("Server is checking for messages...");
        int received_host_ID;
        int received_connection_ID;
        int received_channel_ID;
        int recieved_data_size;
        byte[] buffer = new byte[99];
        int data_size = 99;

        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        // Poll both server/client events

        networkEvent = NetworkTransport.Receive(out received_host_ID,
                                                out received_connection_ID,
                                                out received_channel_ID,
                                                buffer,
                                                data_size,
                                                out recieved_data_size,
                                                out error
                                                );

        switch (networkEvent)
        {
            case NetworkEventType.Nothing:
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Server Recieved a Connection Event");
                server_players_amount++;
                Debug.Log("Player " + server_players_amount.ToString() + " Joined");

                join("Player " + server_players_amount.ToString());
                //Debug.Log(received_host_ID.ToString());
                //Debug.Log("ServerConnection Before: " + server_client_connection);
                server_client_connection[server_players_amount - 1] = received_connection_ID;
                //Debug.Log("ServerConnection After: " + server_client_connection);
                server_confirm_client_join(received_connection_ID, server_players_amount);

                break;
        }
    }





    void server_confirm_client_join(int s_c_connection, byte players_in_game)
    {
        Debug.Log("ServerConnection After: " + server_client_connection);
        byte error;
        byte[] message = new byte[99];
        //
        //
        //
        //

        //
        //
        // cant use client_joined only known by client and this is the server code
        message[0] = 1;
        // Update client on how many people joined
        message[1] = players_in_game;
        message[2] = 0;


        NetworkTransport.Send(server_socket_ID, s_c_connection, server_reliable_channel, message, 99, out error);


        if (error != 0)
        {
            Debug.Log("Could not send");
            Debug.Log(error.ToString());
        }
        else
        {
            Debug.Log("SENT");
            ///Debug.Log("IM HERERERER 3");
        }

    }




    void client_lobby_update()
    {
        byte error;
        //Debug.Log("Server is checking for messages...");
        int received_host_ID;
        int received_connection_ID;
        int received_channel_ID;
        int recieved_data_size;
        byte[] buffer = new byte[99];
        int data_size = 99;

        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        // Poll both server/client events

        networkEvent = NetworkTransport.Receive(out received_host_ID,
                                                out received_connection_ID,
                                                out received_channel_ID,
                                                buffer,
                                                data_size,
                                                out recieved_data_size,
                                                out error
                                                );

        switch (networkEvent)
        {
            case NetworkEventType.Nothing:
                //Debug.Log("...");
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Its getting a connection event...Now?");
                break;
            case NetworkEventType.DataEvent:
                Debug.Log("Its getting data");

                if (buffer[2] == 0)
                {
                    client_joined = true;
                    client_players_amount = buffer[1];
                    Debug.Log("Number of Players in Lobby: " + client_players_amount.ToString());

                    // Open up a joined canvas for the client
                    GameObject custom_network_manager = GameObject.Find("Game Manager(Clone)");
                    manager_script = custom_network_manager.GetComponent<Canvas_Manager>();
                    manager_script.waiting_in_lobby(client_players_amount);
                }

                if (buffer[2] == 1)
                {
                    Debug.Log("The Server is telling me to start the game");

                    GameObject g_manager = GameObject.Find("Game Manager(Clone)");
                    Canvas_Manager c_manager_script = g_manager.GetComponent<Canvas_Manager>();
                    c_manager_script.start_the_game();
                }

                break;
        }
    }




    void tell_clients_to_start()
    {
        byte error;
        byte[] message = new byte[99];
        //
        //
        //
        //

        //
        //
        // cant use client_joined only known by client and this is the server code
        message[0] = 1;
        // Update client on how many people joined
        message[1] = 0;
        message[2] = 1;


        NetworkTransport.Send(server_socket_ID, server_client_connection[1], server_reliable_channel, message, 99, out error);
        NetworkTransport.Send(server_socket_ID, server_client_connection[2], server_reliable_channel, message, 99, out error);
        NetworkTransport.Send(server_socket_ID, server_client_connection[3], server_reliable_channel, message, 99, out error);

        if (error != 0)
        {
            Debug.Log("Could not send");
            Debug.Log(error.ToString());
        }
        else
        {
            Debug.Log("SENT");
            ///Debug.Log("IM HERERERER 3");
        }
    }


    public bool is_the_host()
    {
        return is_host;
    }






    public void client_send_information()
    {
        byte error;
        NetworkTransport.Send(client_socket_ID, client_connection, client_reliable_channel, client_info, 24, out error);

    }



    void server_get_client_player_data()
    {
        byte error;
        //Debug.Log("Server is checking for messages...");
        int received_host_ID;
        int received_connection_ID;
        int received_channel_ID;
        int recieved_data_size;
        byte[] buffer = new byte[24];
        int data_size = 24;

        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        // Poll both server/client events

        networkEvent = NetworkTransport.Receive(out received_host_ID,
                                                out received_connection_ID,
                                                out received_channel_ID,
                                                buffer,
                                                data_size,
                                                out recieved_data_size,
                                                out error
                                                );

        switch (networkEvent)
        {
            case NetworkEventType.Nothing:

                //server_player_control = server_client_connection[0];
                break;
            case NetworkEventType.ConnectEvent:
                Debug.Log("Server Recieved a Connection Event....here?");
                break;
            case NetworkEventType.DataEvent:
                Debug.Log("Recieved data from Player 2");
                if (received_channel_ID == server_real_reliable_channel)
                {
                    reliable_message = true;
                }
                else
                {
                    reliable_message = false;
                }
                //Debug.Log("WE MADE IT!!!!!");
                //float[] back = new float[3];
                //Buffer.BlockCopy(buffer, 0, back, 0, 12);
                //Debug.Log(back[0].ToString());
                //Debug.Log(back[1].ToString());
                //Debug.Log(back[2].ToString());

                server_update_world(buffer, received_connection_ID);
                break;
        }
    }





    void server_update_world(byte[] client_inputs, int player_connection)
    {
        server_to_client_data = client_inputs;
        server_player_control = player_connection + 1;

        //GameObject player = GameObject.Find("Player(Clone)");
        //PlayerController player_script = player.GetComponent<PlayerController>();
        //player_script.server_update_world(client_inputs);
    }







    void client_get_large_message_from_server()
    {
        byte error;
        //Debug.Log("Server is checking for messages...");
        int received_host_ID;
        int received_connection_ID;
        int received_channel_ID;
        int recieved_data_size;
        byte[] buffer = new byte[99];
        int data_size = 99;

        NetworkEventType networkEvent = NetworkEventType.DataEvent;

        // Poll both server/client events

        networkEvent = NetworkTransport.Receive(out received_host_ID,
                                                out received_connection_ID,
                                                out received_channel_ID,
                                                buffer,
                                                data_size,
                                                out recieved_data_size,
                                                out error
                                                );

        switch (networkEvent)
        {
            case NetworkEventType.Nothing:
                //Debug.Log("No Message");
                break;
            case NetworkEventType.ConnectEvent:
                break;
            case NetworkEventType.DataEvent:
                //Debug.Log(NetworkTransport.GetCurrentRtt(received_host_ID, client_connection, out error).ToString());
                //Debug.Log("I am the client and I am getting a large message of size: " + data_size.ToString());
                server_to_client_data_large = buffer;
                break;
        }
    }



    void server_send_large_message_to_client()
    {
        byte error;
        NetworkTransport.Send(server_socket_ID,
                              server_client_connection[1],
                              server_reliable_channel,
                              server_to_client_data_large,
                              99,
                              out error);

        NetworkTransport.Send(server_socket_ID,
                              server_client_connection[2],
                              server_reliable_channel,
                              server_to_client_data_large,
                              99,
                              out error);

        NetworkTransport.Send(server_socket_ID,
                              server_client_connection[3],
                              server_reliable_channel,
                              server_to_client_data_large,
                              99,
                              out error);

    }


    public byte getServerPlayersAmt()
    {
        return server_players_amount;
    }





    public void server_send_reliable()
    {
        byte error;
        server_reliable_buffer[0] = 1;
        NetworkTransport.Send(server_socket_ID, server_client_connection[1], server_real_reliable_channel, server_reliable_buffer, 99, out error);

    }


    public void client_send_reliable()
    {
        byte error;
        client_reliable_buffer[0] = 1;
        NetworkTransport.Send(client_socket_ID, client_connection, server_reliable_channel, client_reliable_buffer, 24, out error);

    }

}