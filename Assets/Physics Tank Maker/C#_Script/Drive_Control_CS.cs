using UnityEngine;
using System.Collections;

public class Drive_Control_CS : MonoBehaviour
{

    public float Torque = 400.0f;
    public float Max_Speed = 10.0f;
    public float Turn_Brake_Drag = 100.0f;
    public float MaxAngVelocity_Limit = 45.0f;

    public bool Acceleration_Flag = false;
    public float Acceleration_Time = 4.0f;
    public float Deceleration_Time = 2.0f;
    public float Lost_Drag_Rate = 0.85f;
    public float Lost_Speed_Rate = 0.3f;

    public bool Torque_Limitter = false;
    public float Max_Slope_Angle = 35.0f;

    public float ParkingBrake_Velocity = 0.5f;
    public float ParkingBrake_Lag = 0.5f;
    public bool Fix_Useless_Rotaion = false;

    // Referred to from Drive_Wheel.
    public bool Parking_Brake = false;
    public float L_Temp;
    public float R_Temp;
    public float L_Brake;
    public float R_Brake;
    public float L_Speed_Rate;
    public float R_Speed_Rate;
    public bool L_Forward_Flag;
    public bool R_Forward_Flag;

    // Referred to from Steer_Wheel.
    public bool Stop_Flag = true;

    int Turn_Type = 0;
    float Vertical;
    float Horizontal;
    int Speed_Step;
    int Turn_Step;
    float Default_Torque;
    float Acceleration_Rate;
    float Deceleration_Rate;
    float R_Forward_Rate;
    float R_Backward_Rate;
    float L_Forward_Rate;
    float L_Backward_Rate;

    float Still_Count;

    Rigidbody MainBody_Rigidbody;
    Transform This_Transform;

    bool Flag = true;
    int Tank_ID;
    int Input_Type = 4;

    AI_CS AI_Script;
    Drive_Control_CS Control_Script;

    void Start()
    {
        This_Transform = transform;
        Default_Torque = Torque;
        if (Acceleration_Flag)
        {
            Acceleration_Rate = 1.0f / Acceleration_Time;
            Deceleration_Rate = 1.0f / Deceleration_Time;
        }
        BroadcastMessage("Get_Drive_Control", this, SendMessageOptions.DontRequireReceiver);
        MainBody_Rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Flag)
        {
            switch (Input_Type)
            {
                case 0:
                    KeyBoard_Input();
                    break;
                case 1:
                    Stick_Input();
                    break;
                case 2:
                    Trigger_Input();
                    break;
                case 3:
                    Stick_Trigger_Input();
                    break;
                case 4:
                    Mouse_Input();
                    break;
                case 5:
                    Mouse_Input_Easy();
                    break;
                case 10:
                    AI_Input();
                    break;
            }
        }
        // Calculate Acceleration.
        if (Acceleration_Flag)
        {
            Acceleration();
        }
        // Limit the Torque in slope.
        if (Torque_Limitter)
        {
            Limit_Torque();
        }
    }

    void FixedUpdate()
    {
        // Parking Brake Control.
        if (Stop_Flag)
        {
            float Temp_Velocity_Magnitude = MainBody_Rigidbody.velocity.magnitude;
            float Temp_AngularVelocity_Magnitude = MainBody_Rigidbody.angularVelocity.magnitude;
            if (Parking_Brake == false)
            {
                if (Temp_Velocity_Magnitude < ParkingBrake_Velocity && Temp_AngularVelocity_Magnitude < ParkingBrake_Velocity)
                {
                    Still_Count += Time.fixedDeltaTime;
                    if (Still_Count > ParkingBrake_Lag)
                    {
                        Parking_Brake = true;
                        MainBody_Rigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;
                    }
                }
            }
            else
            {
                if (Temp_Velocity_Magnitude > ParkingBrake_Velocity || Temp_AngularVelocity_Magnitude > ParkingBrake_Velocity)
                {
                    Parking_Brake = false;
                    MainBody_Rigidbody.constraints = RigidbodyConstraints.None;
                    Still_Count = 0.0f;
                }
            }
        }
        else
        {
            Parking_Brake = false;
            MainBody_Rigidbody.constraints = RigidbodyConstraints.None;
            Still_Count = 0.0f;
        }
    }

    void Acceleration()
    {
        // Switch Rate.
        if (R_Forward_Flag)
        {
            R_Speed_Rate = R_Forward_Rate;
        }
        else
        {
            R_Speed_Rate = R_Backward_Rate;
        }
        if (L_Forward_Flag)
        {
            L_Speed_Rate = L_Forward_Rate;
        }
        else
        {
            L_Speed_Rate = L_Backward_Rate;
        }
        // Right
        if (R_Temp > 0.0f)
        { // Forward
            if (R_Backward_Rate == 0.0f)
            {
                R_Forward_Rate = Calculate_Speed_Rate(R_Forward_Rate, R_Temp);
                R_Forward_Flag = true;
            }
            else
            {
                R_Backward_Rate = Calculate_Speed_Rate(R_Backward_Rate, 0.0f);
                R_Forward_Flag = false;
            }
        }
        else if (R_Temp < 0.0f)
        { // Backward
            if (R_Forward_Rate == 0.0f)
            {
                R_Backward_Rate = Calculate_Speed_Rate(R_Backward_Rate, R_Temp);
                R_Forward_Flag = false;
            }
            else
            {
                R_Forward_Rate = Calculate_Speed_Rate(R_Forward_Rate, 0.0f);
                R_Forward_Flag = true;
            }
        }
        else
        { // To stop ( R_Temp == 0 ).
            if (R_Backward_Rate != 0.0f)
            {
                R_Backward_Rate = Calculate_Speed_Rate(R_Backward_Rate, 0.0f);
            }
            if (R_Forward_Rate != 0.0f)
            {
                R_Forward_Rate = Calculate_Speed_Rate(R_Forward_Rate, 0.0f);
            }
        }
        // Left
        if (L_Temp < 0.0f)
        { // Forward
            if (L_Backward_Rate == 0.0f)
            {
                L_Forward_Rate = Calculate_Speed_Rate(L_Forward_Rate, L_Temp);
                L_Forward_Flag = true;
            }
            else
            {
                L_Backward_Rate = Calculate_Speed_Rate(L_Backward_Rate, 0.0f);
                L_Forward_Flag = false;
            }
        }
        else if (L_Temp > 0.0f)
        { // Backward
            if (L_Forward_Rate == 0.0f)
            {
                L_Backward_Rate = Calculate_Speed_Rate(L_Backward_Rate, L_Temp);
                L_Forward_Flag = false;
            }
            else
            {
                L_Forward_Rate = Calculate_Speed_Rate(L_Forward_Rate, 0.0f);
                L_Forward_Flag = true;
            }
        }
        else
        { // To stop ( L_Temp == 0 ).
            if (L_Backward_Rate != 0.0f)
            {
                L_Backward_Rate = Calculate_Speed_Rate(L_Backward_Rate, 0.0f);
            }
            if (L_Forward_Rate != 0.0f)
            {
                L_Forward_Rate = Calculate_Speed_Rate(L_Forward_Rate, 0.0f);
            }
        }
        // Stabilize.
        float Temp = Mathf.Abs(L_Temp + R_Temp);
        if (Temp < 0.1f)
        {
            if (L_Speed_Rate != R_Speed_Rate)
            {
                Temp = (L_Speed_Rate + R_Speed_Rate) / 2.0f;
                L_Speed_Rate = Temp;
                R_Speed_Rate = Temp;
                //
                if (L_Forward_Rate != R_Forward_Rate)
                {
                    Temp = (L_Forward_Rate + R_Forward_Rate) / 2.0f;
                    L_Forward_Rate = Temp;
                    R_Forward_Rate = Temp;
                }
                if (L_Backward_Rate != R_Backward_Rate)
                {
                    Temp = (L_Backward_Rate + R_Backward_Rate) / 2.0f;
                    L_Backward_Rate = Temp;
                    R_Backward_Rate = Temp;
                }
            }
        }
    }

    float Calculate_Speed_Rate(float Speed_Rate, float Temp)
    {
        float Target_Rate = Mathf.Abs(Temp);
        if (Speed_Rate < Target_Rate)
        {
            Speed_Rate = Mathf.MoveTowards(Speed_Rate, Target_Rate, Acceleration_Rate * Time.deltaTime);
        }
        else if (Speed_Rate > Target_Rate)
        {
            Speed_Rate = Mathf.MoveTowards(Speed_Rate, Target_Rate, Deceleration_Rate * Time.deltaTime);
        }
        return Speed_Rate;
    }

    void Limit_Torque()
    {
        float Temp_X = Mathf.DeltaAngle(This_Transform.eulerAngles.x, 0.0f) / Max_Slope_Angle;
        if (L_Temp < 0.0f && R_Temp > 0.0f)
        { // forward
            Torque = Mathf.Lerp(Default_Torque, 0.0f, Temp_X);
        }
        else
        { // backward
            Torque = Mathf.Lerp(Default_Torque, 0.0f, -Temp_X);
        }
        if (Torque == 0.0f)
        {
            L_Temp = 0.0f;
            R_Temp = 0.0f;
            L_Forward_Rate = 0.0f;
            R_Forward_Rate = 0.0f;
            L_Backward_Rate = 0.0f;
            R_Backward_Rate = 0.0f;
        }
    }

    void KeyBoard_Input()
    {
        if (Input.GetKey("z") == false && Input.GetKey("c") == false)
        {
            Vertical = Input.GetAxis("Vertical");
            Horizontal = Input.GetAxis("Horizontal");
            Basic_Drive();
        }
        else
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
        }
    }

    void Stick_Input()
    {
        if (Input.GetButton("Jump") == false)
        {
            Vertical = Input.GetAxis("Vertical");
            Horizontal = Input.GetAxis("Horizontal");
            Basic_Drive();
        }
        else
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
        }
    }

    void Basic_Drive()
    {
        if (Vertical == 0.0f && Horizontal == 0.0f)
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
        }
        else
        {
            Stop_Flag = false;
            switch (Turn_Type)
            {
                case 0:
                    Easy_Turn();
                    break;
                case 1:
                    Classic_Turn();
                    break;
            }
        }
    }

    void Easy_Turn()
    {
        if (Mathf.Abs(Vertical) == 0.0f)
        { // Pivot Turn
            L_Temp = -Horizontal;
            R_Temp = -Horizontal;
            L_Brake = 0.0f;
            R_Brake = 0.0f;
        }
        else
        { // Brake Turn
            L_Temp = -Vertical;
            R_Temp = Vertical;
            if (Horizontal < 0.0f)
            {
                L_Brake = -Horizontal;
                R_Brake = 0.0f;
            }
            else if (Horizontal > 0.0f)
            {
                L_Brake = 0.0f;
                R_Brake = Horizontal;
            }
            else
            { // No Turn
                L_Brake = 0.0f;
                R_Brake = 0.0f;
            }
        }
    }

    void Classic_Turn()
    {
        L_Temp = -Vertical;
        R_Temp = Vertical;
        if (Horizontal < 0.0f)
        {
            L_Brake = -Horizontal;
            R_Brake = 0.0f;
        }
        else if (Horizontal > 0.0f)
        {
            L_Brake = 0.0f;
            R_Brake = Horizontal;
        }
        else
        {
            L_Brake = 0.0f;
            R_Brake = 0.0f;
        }
    }

    void Trigger_Input()
    {
        float L_Temp_Trigger = -Input.GetAxis("L_Trigger");
        float R_Temp_Trigger = Input.GetAxis("R_Trigger");
        float L_Temp_Button = +Input.GetAxis("L_Button");
        float R_Temp_Button = -Input.GetAxis("R_Button");
        if (L_Temp_Trigger >= 0.0f && R_Temp_Trigger <= 0.0f && L_Temp_Button <= 0.0f && R_Temp_Button >= 0.0f)
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
            L_Brake = 0.0f;
            R_Brake = 0.0f;
        }
        else
        {
            Stop_Flag = false;
            if (L_Temp_Button > 0.0f && L_Temp_Trigger >= 0.0f)
            {
                L_Temp = L_Temp_Button;
                L_Brake = 1.0f - L_Temp_Button;
            }
            else if (L_Temp_Trigger < 0.0f && L_Temp_Button == 0.0f)
            {
                L_Temp = -1.0f;
                //L_Brake = Mathf.Lerp ( 1.0f , 0.0f , Mathf.Sqrt ( -L_Temp_Trigger ) ) ;
                L_Brake = 1.0f + L_Temp_Trigger;
            }
            else
            {
                if (R_Temp != 0.0f)
                {
                    L_Temp = R_Temp;
                }
                else
                {
                    L_Temp = 0.0f;
                }
                L_Brake = 1.0f;
            }
            if (R_Temp_Button < 0.0f && R_Temp_Trigger <= 0.0f)
            {
                R_Temp = R_Temp_Button;
                R_Brake = 1.0f + R_Temp_Button;
            }
            else if (R_Temp_Trigger > 0.0f && R_Temp_Button == 0.0f)
            {
                R_Temp = 1.0f;
                //R_Brake = Mathf.Lerp ( 1.0f , 0.0f , Mathf.Sqrt ( R_Temp_Trigger ) ) ;
                R_Brake = 1.0f - R_Temp_Trigger;
            }
            else
            {
                if (L_Temp != 0.0f)
                {
                    R_Temp = L_Temp;
                }
                else
                {
                    R_Temp = 0.0f;
                }
                R_Brake = 1.0f;
            }
        }
    }

    void Stick_Trigger_Input()
    {
        if (Input.GetButton("Jump") == false)
        {
            Vertical = Input.GetAxis("R_Trigger") - Input.GetAxis("L_Trigger");
            Horizontal = Input.GetAxis("Horizontal");
            if (Vertical == 0.0f)
            {
                if (Horizontal == 0.0f)
                {
                    Stop_Flag = true;
                    L_Temp = 0.0f;
                    R_Temp = 0.0f;
                    L_Brake = 0.0f;
                    R_Brake = 0.0f;
                    return;
                }
                else
                {
                    Stop_Flag = false;
                    L_Temp = -Horizontal;
                    R_Temp = -Horizontal;
                    L_Brake = 0.0f;
                    R_Brake = 0.0f;
                    return;
                }
            }
            else
            {
                Stop_Flag = false;
                L_Temp = -Vertical;
                R_Temp = Vertical;
                if (Horizontal < 0.0f)
                {
                    L_Brake = -Horizontal;
                    R_Brake = 0.0f;
                }
                else if (Horizontal > 0.0f)
                {
                    L_Brake = 0.0f;
                    R_Brake = Horizontal;
                }
                else
                {
                    L_Brake = 0.0f;
                    R_Brake = 0.0f;
                }
            }
        }
        else
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
            L_Brake = 0.0f;
            R_Brake = 0.0f;
        }
    }

    void Mouse_Input()
    {
        if (Input.anyKey)
        {
            // Input Speed
            if (Input.GetKeyDown("w"))
            {
                Speed_Step += 1;
            }
            else if (Input.GetKeyDown("s"))
            {
                Speed_Step -= 1;
            }
            else if (Input.GetKey("x"))
            {
                L_Temp = 0.0f;
                R_Temp = 0.0f;
                L_Brake = 1.0f;
                R_Brake = 1.0f;
                Speed_Step = 0;
                L_Forward_Rate = 0.0f;
                R_Forward_Rate = 0.0f;
                L_Backward_Rate = 0.0f;
                R_Backward_Rate = 0.0f;
                return;
            }
            Speed_Step = Mathf.Clamp(Speed_Step, -2, 4);
            // Input Turn
            if (Input.GetKey("q"))
            { //Smooth Left Turn
                Turn_Step = 1;
            }
            else if (Input.GetKey("e"))
            { //Smooth Right Turn
                Turn_Step = 2;
            }
            else if (Input.GetKey("a"))
            { // Brake Left Turn
                Turn_Step = 3;
            }
            else if (Input.GetKey("d"))
            { //Brake Right Turn
                Turn_Step = 4;
            }
            else if (Input.GetKey("z"))
            { //Pivot Left Turn
                Turn_Step = 5;
            }
            else if (Input.GetKey("c"))
            { //Pivot Right Turn
                Turn_Step = 6;
            }
            else
            { // No Turn
                Turn_Step = 0;
            }
        }
        else
        {
            Turn_Step = 0;
        }
        Speed_Turn_Control();
    }

    void Speed_Turn_Control()
    {
        if (Speed_Step == 0 && Turn_Step == 0)
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
            return;
        }
        else
        {
            Stop_Flag = false;
            switch (Turn_Step)
            {
                case 1: // Smooth Left Turn
                    L_Temp = -Speed_Step / 4.0f;
                    R_Temp = Speed_Step / 4.0f;
                    L_Brake = 0.15f;
                    R_Brake = 0.0f;
                    break;
                case 2: // Smooth Right Turn
                    L_Temp = -Speed_Step / 4.0f;
                    R_Temp = Speed_Step / 4.0f;
                    L_Brake = 0.0f;
                    R_Brake = 0.15f;
                    break;
                case 3: // Brake Left Turn
                    L_Temp = -Speed_Step / 4.0f;
                    R_Temp = Speed_Step / 4.0f;
                    L_Brake = -Lost_Drag_Rate * L_Speed_Rate + 1.0f;
                    R_Brake = 0.0f;
                    if (L_Forward_Flag)
                    {
                        float Temp_Rate = L_Forward_Rate - ((L_Forward_Rate * Lost_Speed_Rate) * Time.deltaTime);
                        L_Forward_Rate = Temp_Rate;
                        R_Forward_Rate = Temp_Rate;
                    }
                    else
                    {
                        float Temp_Rate = L_Backward_Rate - ((L_Backward_Rate * Lost_Speed_Rate) * Time.deltaTime);
                        L_Backward_Rate = Temp_Rate;
                        R_Backward_Rate = Temp_Rate;
                    }
                    break;
                case 4: // Brake Right Turn
                    L_Temp = -Speed_Step / 4.0f;
                    R_Temp = Speed_Step / 4.0f;
                    L_Brake = 0.0f;
                    R_Brake = -Lost_Drag_Rate * R_Speed_Rate + 1.0f;
                    if (R_Forward_Flag)
                    {
                        float Temp_Rate = R_Forward_Rate - ((R_Forward_Rate * Lost_Speed_Rate) * Time.deltaTime);
                        L_Forward_Rate = Temp_Rate;
                        R_Forward_Rate = Temp_Rate;
                    }
                    else
                    {
                        float Temp_Rate = R_Backward_Rate - ((R_Backward_Rate * Lost_Speed_Rate) * Time.deltaTime);
                        L_Backward_Rate = Temp_Rate;
                        R_Backward_Rate = Temp_Rate;
                    }
                    break;
                case 5: // Pivot Left Turn
                    L_Temp = 0.5f;
                    R_Temp = 0.5f;
                    L_Brake = 0.0f;
                    R_Brake = 0.0f;
                    break;
                case 6: // Pivot Right Turn
                    L_Temp = -0.5f;
                    R_Temp = -0.5f;
                    L_Brake = 0.0f;
                    R_Brake = 0.0f;
                    break;
                default: // No Turn
                    L_Temp = -Speed_Step / 4.0f;
                    R_Temp = Speed_Step / 4.0f;
                    L_Brake = 0.0f;
                    R_Brake = 0.0f;
                    break;
            }
        }
    }

    void Mouse_Input_Easy()
    {
        if (Input.anyKey)
        {
            if (Input.GetKey("w"))
            {
                Vertical = 1.0f;
            }
            else if (Input.GetKey("s"))
            {
                Vertical = -1.0f;
            }
            else
            {
                Vertical = 0.0f;
            }
            if (Input.GetKey("x"))
            {
                L_Temp = 0.0f;
                R_Temp = 0.0f;
                L_Brake = 1.0f;
                R_Brake = 1.0f;
                L_Forward_Rate = 0.0f;
                R_Forward_Rate = 0.0f;
                L_Backward_Rate = 0.0f;
                R_Backward_Rate = 0.0f;
                return;
            }
            else if (Input.GetKey("a"))
            {
                Horizontal = -1.0f;
            }
            else if (Input.GetKey("d"))
            {
                Horizontal = 1.0f;
            }
            else
            {
                Horizontal = 0.0f;
            }
        }
        else
        {
            Vertical = 0.0f;
            Horizontal = 0.0f;
        }
        Basic_Drive();
    }

    void AI_Input()
    {
        Vertical = AI_Script.Speed_Order;
        Horizontal = AI_Script.Turn_Order;
        if (Vertical == 0.0f && Horizontal == 0.0f)
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
        }
        else
        {
            Stop_Flag = false;
            L_Temp = Mathf.Clamp(-Vertical - Horizontal, -1.0f, 1.0f);
            R_Temp = Mathf.Clamp(Vertical - Horizontal, -1.0f, 1.0f);
        }
    }


    void Set_Input_Type(int Temp_Input_Type)
    {
        Input_Type = Temp_Input_Type;
    }

    void Set_Turn_Type(int Temp_Type)
    {
        Turn_Type = Temp_Type;
    }

    void Set_Tank_ID(int Temp_Tank_ID)
    {
        Tank_ID = Temp_Tank_ID;
    }

    void Receive_Current_ID(int Temp_Current_ID)
    {
        if (Temp_Current_ID == Tank_ID)
        {
            Flag = true;
        }
        else
        {
            if (Input_Type == 10)
            {
                Flag = true;
            }
            else
            {
                Flag = false;
            }
        }
    }

    void Get_AI(AI_CS Temp_Script)
    {
        AI_Script = Temp_Script;
    }

    void MainBody_Linkage()
    { // Called from MainBody's "Damage_Control".
        Destroy(this);
    }

}
