using UnityEngine;
using System.Collections;

/// <summary>
/// THIS IS OWNED BY PLAYER 2/Client
/// </summary>

public class Turret_Controller_VR : MonoBehaviour 
{
    byte current_player; // owner = player 2


    // Client Queue
    int frame = 0;

    int server_player;

    // general
    float turret_position_x;
    float turret_position_y;
    float turret_position_z;

    float turret_rotation_x;
    float turret_rotation_y;
    float turret_rotation_z;

    float turret_base_rotation_y;
    float cannon_base_rotation_x;





    // Moving Barrel
    float turns = 6.0f;
    float vertical_lower_bound_angle = 5.0f;
    float vertical_upper_bound_angle = -25.0f;
    float vertical_angle_range = 30.0f;

    float turn_ratio = 30.0f / 6.0f;

    float max_positive = 1800.0f;
    float max_negative = -360.0f;
    float location_angle = 0.0f;

    float location_in_range;
    float past_angle;
    bool increasing;
    float location_adder;


    // Moving the Turret
    float number_of_turns = 20;
    float max_angle_range;
    float min_angle_range;

    float tally_positive;
    float tally_negative;
    float previous_angle;

    



    //trigger

    GameObject n_manager;
    network_manager n_manager_script;
    public GameObject turret_objects;
    public GameObject cannon_base;
    Control_Angles control_angles;
    Cannon_Vertical_CS cannon_vertical;

    bool started = false;
    bool ready = false;

    bool reliable_message = false;

    int frame_interval = 5;

    public void Prep() {
        n_manager = GameObject.Find("Custom Network Manager(Clone)");
        n_manager_script = n_manager.GetComponent<network_manager>();
        current_player = (byte)(n_manager_script.client_players_amount);
        if (current_player != 2) {
            cannon_vertical.enabled = false;
            //this.GetComponent<Drive_Control_CS>().enabled = false;
            //BroadcastMessage("DisableDriveWheel");
        }
    }

    void Start() {
        control_angles = GetComponent<Control_Angles>();
        cannon_vertical = cannon_base.GetComponent<Cannon_Vertical_CS>();

    }

    void Update() {

        if (n_manager != null) {
            started = n_manager_script.started;
            ready = n_manager_script.game_ready;

            server_player = n_manager_script.server_player_control;

            reliable_message = n_manager_script.reliable_message;

            if (current_player == 2) {
                Move_Turret();
                client_send_values();
                
            } 
            else {
                server_get_client_hands();
            }


        }
    }

    void FixedUpdate() {
        if (n_manager != null) {
            update_world_state();
        }
    }

    void Move_Turret() {
        cannon_vertical.Temp_Vertical = control_angles.GetVertCrankDelta() / 20f;
    }



    void update_world_state() {
        if (current_player == 2) { 
            //
        } else {
            cannon_base.transform.localRotation = Quaternion.Euler(cannon_base_rotation_x, 0, 0);
            turret_objects.transform.localRotation = Quaternion.Euler(0, turret_base_rotation_y, 0);
        }
    }


    public byte get_client_player_number() {
        return current_player;
    }


    public void add_trigger_listener() {
        //right_controller.GetComponent<VRTK.VRTK_ControllerEvents>().TriggerClicked += new VRTK.ControllerInteractionEventHandler(client_send_reliable_message);
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
        float[] cannon_base_rotation_values = { cannon_base.transform.localRotation.x,
                                                0,
                                                0};
        /*
        float[] hull_rotation_values = { transform.localRotation.eulerAngles.x,
                                         transform.localRotation.eulerAngles.y,
                                         transform.localRotation.eulerAngles.z };
        */
        Debug.Log("Client Buffer Values: Putting In");
        Debug.Log(cannon_base_rotation_values.ToString());
        n_manager_script.send_from_client(6, cannon_base_rotation_values);
        //n_manager_script.send_from_client(4, hull_rotation_values);

    }



    // Server Updates the server larger buffer it is going to send
    public void server_get_values_to_send() {

        float[] cannon_base_rotation_values = { cannon_base.transform.localRotation.x,
                                                0,
                                                0};
        /*
        float[] hull_rotation_values = { transform.localRotation.eulerAngles.x,
                                         transform.localRotation.eulerAngles.y,
                                         transform.localRotation.eulerAngles.z };
        */

        n_manager_script.send_from_server(6, cannon_base_rotation_values);
        //n_manager_script.send_from_server(4, hull_rotation_values);

    }




    // Client get values from the server buffer
    void client_update_values() {

        float[] cannon_base_rotation_values = n_manager_script.client_read_server_buffer(6);
        //float[] hull_rotation_values = n_manager_script.client_read_server_buffer(4);


        cannon_base_rotation_x = cannon_base_rotation_values[0];
         /*
        pos_x = hull_position_values[0];
        pos_y = hull_position_values[1];
        pos_z = hull_position_values[2];

        rot_x = hull_rotation_values[0];
        rot_y = hull_rotation_values[1];
        rot_z = hull_rotation_values[2];
        */


    }

    // Server Get values from the client buffer, so the client inputs
    public void server_get_client_hands() {
        float[] cannon_base_rotation_values = n_manager_script.server_read_client_buffer(6);
        Debug.Log("Server Buffer Values: Taking Out");
        Debug.Log(cannon_base_rotation_values.ToString());
        cannon_base_rotation_x = cannon_base_rotation_values[0];
        //float[] hull_rotation_values = n_manager_script.server_read_client_buffer(4);

        //pos_x = hull_position_values[0];
        //pos_y = hull_position_values[1];
        //pos_z = hull_position_values[2];

        //rot_x = hull_rotation_values[0];
        //rot_y = hull_rotation_values[1];
        //rot_z = hull_rotation_values[2];

    }
}