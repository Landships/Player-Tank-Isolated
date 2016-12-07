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
    float barrel_base_rotation_x;





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
    public GameObject barrel_base;
    Control_Angles control_angles;

    bool started = false;
    bool ready = false;

    bool reliable_message = false;

    int frame_interval = 5;

    public void Prep() {
        n_manager = GameObject.Find("Custom Network Manager(Clone)");
        n_manager_script = n_manager.GetComponent<network_manager>();
        current_player = (byte)(n_manager_script.client_players_amount);
        if (current_player != 1) {
            //this.GetComponent<Drive_Control_CS>().enabled = false;
            //BroadcastMessage("DisableDriveWheel");
        }
    }

    void Start() {
        control_angles = GetComponent<Control_Angles>();
        past_angle = 90.0f;
        increasing = true;
        location_in_range = 0.0f;
        location_adder = 5.0f;


        max_angle_range = 360.0f * number_of_turns;
        min_angle_range = -max_angle_range;

        tally_negative = 0;
        tally_positive = 0;
        previous_angle = 0.0f;

    }

    void Update() {

        if (n_manager != null) {
            started = n_manager_script.started;
            ready = n_manager_script.game_ready;

            server_player = n_manager_script.server_player_control;

            reliable_message = n_manager_script.reliable_message;

            if (current_player == 1) {
                server_get_values_to_send();
            } else {
                client_update_values();
            }

            Move_Turret();
        }
    }

    void FixedUpdate() {
        if (n_manager != null) {
            update_world_state();
        }
    }

    void Move_Turret() {
        float horizontal_crank_angle = control_angles.GetLeftCrankAngle();
        float vertical_crank_angle = control_angles.GetRightCrankAngle();

        if (horizontal_crank_angle < 0) {
            horizontal_crank_angle = 180 + (180 + horizontal_crank_angle); 
        }

        Debug.Log(horizontal_crank_angle.ToString());



        // Move Barrel
        if (vertical_crank_angle < 0) {
            vertical_crank_angle = Mathf.Abs(vertical_crank_angle);
        }
        if (vertical_crank_angle >= 0) {
            vertical_crank_angle = vertical_crank_angle + 360.0f;
        }

        barrel_base_rotation_x = (vertical_lower_bound_angle - ((vertical_crank_angle / 2160.0f) * vertical_angle_range));
       

        // Move Gun
        if (previous_angle < 350 && horizontal_crank_angle > 0) {
            tally_positive++;
        }
        if (previous_angle < 5 && horizontal_crank_angle > 350) {
            tally_positive--;
        }
        
        if (previous_angle > 0 && horizontal_crank_angle < 0) {
            tally_negative++;
        }
        
        if (previous_angle < -350 && horizontal_crank_angle > -10) {
            tally_negative++;
        }
        if (previous_angle < 0 && horizontal_crank_angle > 0) {
            tally_negative--;
        }




        if (tally_positive > number_of_turns) {
            tally_positive = 0;
        }
        if (tally_negative < -number_of_turns) {
            tally_negative = 0;
        }


        float alpha_angle = 0;

        if (tally_negative > 0) {
            alpha_angle = alpha_angle + ((tally_negative - 1) * -360.0f);
        }

        if (tally_positive > 0) {
            alpha_angle = alpha_angle + (tally_positive * 360.0f);
        }

        alpha_angle = Mathf.Abs(alpha_angle);

        alpha_angle = alpha_angle / max_angle_range;

        alpha_angle = alpha_angle * 360.0f;


        turret_base_rotation_y = alpha_angle;


        //Debug.Log(turret_base_rotation_y.ToString());

    }



    void update_world_state() {
        if (current_player == 2) { // ITS 2 BUT CHANGE IT LATER BECAUSE PLAYER 2 IS THE GUNNER
            //
        } else {
            barrel_base.transform.localRotation = Quaternion.Euler(barrel_base_rotation_x, 0, 0);
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
        float[] hull_position_values = { transform.localPosition.x,
                                         transform.localPosition.y,
                                         transform.localPosition.z };

        float[] hull_rotation_values = { transform.localRotation.eulerAngles.x,
                                         transform.localRotation.eulerAngles.y,
                                         transform.localRotation.eulerAngles.z };

        //n_manager_script.send_from_client(3, hull_position_values);
        //n_manager_script.send_from_client(4, hull_rotation_values);

    }



    // Server Updates the server larger buffer it is going to send
    public void server_get_values_to_send() {

        float[] hull_position_values = { transform.localPosition.x,
                                         transform.localPosition.y,
                                         transform.localPosition.z };

        float[] hull_rotation_values = { transform.localRotation.eulerAngles.x,
                                         transform.localRotation.eulerAngles.y,
                                         transform.localRotation.eulerAngles.z };


        n_manager_script.send_from_server(3, hull_position_values);
        n_manager_script.send_from_server(4, hull_rotation_values);

    }




    // Client get values from the server buffer
    void client_update_values() {

        float[] hull_position_values = n_manager_script.client_read_server_buffer(3);
        float[] hull_rotation_values = n_manager_script.client_read_server_buffer(4);
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
        float[] hull_position_values = n_manager_script.server_read_client_buffer(3);
        float[] hull_rotation_values = n_manager_script.server_read_client_buffer(4);

        //pos_x = hull_position_values[0];
        //pos_y = hull_position_values[1];
        //pos_z = hull_position_values[2];

        //rot_x = hull_rotation_values[0];
        //rot_y = hull_rotation_values[1];
        //rot_z = hull_rotation_values[2];

    }
}