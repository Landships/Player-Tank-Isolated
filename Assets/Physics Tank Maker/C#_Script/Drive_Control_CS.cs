using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

public class Drive_Control_CS : NetworkBehaviour
{

    public GameObject leftTreadForward;
    public GameObject leftTreadBackward;
    public GameObject rightTreadForward;
    public GameObject rightTreadBackward;
    public float movementThreshold = .5f;
    public GameObject leftSpeedNeedle;
    public GameObject rightSpeedNeedle;
    public Rigidbody rightWheel;
    public Rigidbody leftWheel;
    public Rigidbody mainBody;


    public GameObject leftlever;
    public GameObject rightlever;

    private PlaneLever leftleverscript;
    private PlaneLever rightleverscript;

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
    public bool Fix_Useless_Rotaion = false;

    public float ParkingBrake_Velocity = 0.5f;
    public float ParkingBrake_Lag = 0.5f;

    // Referred to from Drive_Wheel.
    public bool Parking_Brake = false;
    [SyncVar]
    public float L_Temp;
    [SyncVar]
    public float R_Temp;
    public float L_Brake;
    public float R_Brake;
    [SyncVar]
    public float L_Speed_Rate;
    [SyncVar]
    public float R_Speed_Rate;
    public bool L_Forward_Flag;
    public bool R_Forward_Flag;

    // Referred to from Steer_Wheel.
    public bool Stop_Flag = true;

    int Turn_Type = 0;
    float Vertical;
    float Horizontal;
    
    float Left_Speed_Step;
    float Right_Speed_Step;
    int Turn_Step;
    float Default_Torque;
    float Acceleration_Rate;
    float Deceleration_Rate;
    float R_Forward_Rate;
    float R_Backward_Rate;
    float L_Forward_Rate;
    float L_Backward_Rate;
    bool Has_Levers = false;

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
        try {
            leftlever = GameObject.Find("drive_lever_left");
            rightlever = GameObject.Find("drive_lever_right");
            leftleverscript = leftlever.GetComponent<PlaneLever>();
            rightleverscript = rightlever.GetComponent<PlaneLever>();
            GameObject testIfScriptNotNull = leftleverscript.gameObject;
            Has_Levers = true;
        }
        catch (Exception e)
        {
            //Tank has no levers.
            Has_Levers = false;
        }
    }

    void Update()
    {
        if (Flag)
        {
            switch (Input_Type)
            {
                // Removed everything but Mouse+Keyboard input. 
                case 4:
                    Mouse_Input();
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

        //Debug.Log(leftleverscript.leverpos);
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
            else {
                if (Temp_Velocity_Magnitude > ParkingBrake_Velocity || Temp_AngularVelocity_Magnitude > ParkingBrake_Velocity)
                {
                    Parking_Brake = false;
                    MainBody_Rigidbody.constraints = RigidbodyConstraints.None;
                    Still_Count = 0.0f;
                }
            }
        }
        else {
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
        else {
            R_Speed_Rate = R_Backward_Rate;
        }
        if (L_Forward_Flag)
        {
            L_Speed_Rate = L_Forward_Rate;
        }
        else {
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
            else {
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
            else {
                R_Forward_Rate = Calculate_Speed_Rate(R_Forward_Rate, 0.0f);
                R_Forward_Flag = true;
            }
        }
        else { // To stop ( R_Temp == 0 ).
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
            else {
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
            else {
                L_Forward_Rate = Calculate_Speed_Rate(L_Forward_Rate, 0.0f);
                L_Forward_Flag = true;
            }
        }
        else { // To stop ( L_Temp == 0 ).
            if (L_Backward_Rate != 0.0f)
            {
                L_Backward_Rate = Calculate_Speed_Rate(L_Backward_Rate, 0.0f);
            }
            if (L_Forward_Rate != 0.0f)
            {
                L_Forward_Rate = Calculate_Speed_Rate(L_Forward_Rate, 0.0f);
            }
        }

        // Slightly stabilize the speed of the left and right tracks to make controlling the tank less annoying.

        float Temp = Mathf.Abs(L_Temp + R_Temp);
        if (Temp < 0.1f)
        {
            if (L_Speed_Rate != R_Speed_Rate)
            {
                Temp = (L_Speed_Rate + R_Speed_Rate) / 2.0f;
                L_Speed_Rate = Temp;
                R_Speed_Rate = Temp;

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
        else { // backward
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

    float ReCalculate(float angle)
    {
        float newangle = 0;
        if (angle > 180)
        {
            newangle = (angle - 360)/100;
        }
        else
        {
            newangle = angle / 100;
        }
        return newangle;
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
        else {
            Stop_Flag = false;
            L_Temp = Mathf.Clamp(-Vertical - Horizontal, -1.0f, 1.0f);
            R_Temp = Mathf.Clamp(Vertical - Horizontal, -1.0f, 1.0f);
        }
    }


    void Mouse_Input()
    {

        //"Speed Step" is a convention used by the original author of this code.
        // It's a value between -2 and 4 that indicates how fast a certain track is moving. 
        // Originally, there was 1 speed step; I split it into Left_Speed_Step and Right_Speed_Step.


        if (Has_Levers)
        //This is messy. We should move all code inside this conditional out into its own function. 
        {
            float leftangle = leftleverscript.gameObject.transform.localRotation.eulerAngles.x;
            float rightangle = rightleverscript.gameObject.transform.localRotation.eulerAngles.x;

            leftangle = ReCalculate(leftangle);
            rightangle = ReCalculate(rightangle);

            if (leftangle > 0)
            {
                Left_Speed_Step = 4 * leftangle / (leftleverscript.uppercap_degrees/100);
            }
            else if (leftangle < 0)
            {
                Left_Speed_Step = 2 * leftangle / (leftleverscript.lowercap_degrees/100);
            }

            if (rightangle > 0)
            {
                Right_Speed_Step = 4 * rightangle / (rightleverscript.uppercap_degrees / 100);
            }
            else if (rightangle < 0)
            {
                Right_Speed_Step = 2 * rightangle / (rightleverscript.lowercap_degrees / 100);
            }




            // Speedometer
            int forwardReverseAngle = 90;
            int maxAngle = 90;
            int leftDirection;
            int rightDirection;

            rightWheel = GameObject.Find("Invisible_SprocketWheel_R").GetComponent<Rigidbody>();
            leftWheel = GameObject.Find("Invisible_SprocketWheel_L").GetComponent<Rigidbody>();

            Vector3 rightWheelToBody = mainBody.position - rightWheel.position;
            Vector3 leftWheelToBody = mainBody.position - leftWheel.position;

            if (Vector3.Angle(rightWheelToBody, rightWheel.velocity) > forwardReverseAngle)
            {
                rightDirection = 1;
            }
            else {
                rightDirection = -1;
            }
            if (Vector3.Angle(leftWheelToBody, leftWheel.velocity) > forwardReverseAngle)
            {
                leftDirection = 1;
            }
            else {
                leftDirection = -1;
            }

            float rightTargetAngle = rightDirection * (Mathf.Min(rightWheel.velocity.magnitude, Max_Speed) / Max_Speed) * maxAngle;
            float rightCurrAngle = rightSpeedNeedle.GetComponent<RectTransform>().rotation.eulerAngles.z;
            rightSpeedNeedle.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -rightTargetAngle - rightCurrAngle));
            float leftTargetAngle = leftDirection * (Mathf.Min(leftWheel.velocity.magnitude, Max_Speed) / Max_Speed) * maxAngle;
            float leftCurrAngle = leftSpeedNeedle.GetComponent<RectTransform>().rotation.eulerAngles.z;
            leftSpeedNeedle.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -leftTargetAngle - leftCurrAngle));



            // Left forward
            if (Left_Speed_Step > movementThreshold)
            {
                leftTreadForward.SetActive(true);
            }
            else {
                leftTreadForward.SetActive(false);
            }
            // Left backward
            if (Left_Speed_Step < -movementThreshold)
            {
                leftTreadBackward.SetActive(true);
            }
            else {
                leftTreadBackward.SetActive(false);
            }
            // Right forward
            if (Right_Speed_Step > movementThreshold)
            {
                rightTreadForward.SetActive(true);
            }
            else {
                rightTreadForward.SetActive(false);
            }
            // Right backward
            if (Right_Speed_Step < -movementThreshold)
            {
                rightTreadBackward.SetActive(true);
            }
            else {
                rightTreadBackward.SetActive(false);
            }
        }
        

        //Left_Speed_Step += leftleverscript.leverpos;
        //Right_Speed_Step += rightleverscript.leverpos;

        if (Input.anyKey)
        {
            // Input Speed

            /*
            if (Input.GetKey("w"))
            {
                Left_Speed_Step += 1;
            }
            if (Input.GetKey("i"))
            {
                Right_Speed_Step += 1;
            }
            if (Input.GetKey("k"))
            {
                Right_Speed_Step -= 1;
            }
            if (Input.GetKey("s"))
            {
                Left_Speed_Step -= 1;

            }
            */

            
            if (Input.GetKey("x"))
            {
                L_Temp = 0.0f;
                R_Temp = 0.0f;
                L_Brake = 1.0f;
                R_Brake = 1.0f;
                Left_Speed_Step = 0;
                Right_Speed_Step = 0;
                L_Forward_Rate = 0.0f;
                R_Forward_Rate = 0.0f;
                L_Backward_Rate = 0.0f;
                R_Backward_Rate = 0.0f;
                return;
            }

            Left_Speed_Step = Mathf.Clamp(Left_Speed_Step, -2, 4);
            Right_Speed_Step = Mathf.Clamp(Right_Speed_Step, -2, 4);

        }
        else {
            //Decelerate
            Turn_Step = 0;
            Left_Speed_Step = Mathf.Lerp(Left_Speed_Step, Left_Speed_Step / 2, Time.deltaTime);
            Right_Speed_Step = Mathf.Lerp(Right_Speed_Step, Right_Speed_Step / 2, Time.deltaTime);
        }

        Speed_Turn_Control();
    }

    void Speed_Turn_Control()
    {
        // Stopped.
        if (Left_Speed_Step == 0 && Right_Speed_Step == 0 && Turn_Step == 0)
        {
            Stop_Flag = true;
            L_Temp = 0.0f;
            R_Temp = 0.0f;
            L_Brake = 1.00f;
            R_Brake = 1.00f;
            return;
        }

        // Performing an in-place turn; speed should be drastically reduced
        else if (L_Forward_Flag ^ R_Forward_Flag)
        {
            Stop_Flag = false;
            L_Temp = -Left_Speed_Step / 12.0f;
            R_Temp = Right_Speed_Step / 12.0f;
            L_Brake = 0.00f;
            R_Brake = 0.00f;

        }

        else {
            // The division by 4 is taken from the original creator's code.
            // I pretty much built everything 
            Stop_Flag = false;
            // No Turn
            L_Temp = -Left_Speed_Step / 4.0f;
            R_Temp = Right_Speed_Step / 4.0f;
            L_Brake = 0.00f;
            R_Brake = 0.00f;
        }
    }

    bool noButtonsPressed()
    {
        return !Input.GetKey("w") &&
            !Input.GetKey("s") &&
            !Input.GetKey("i") &&
            !Input.GetKey("k");
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
        else {
            if (Input_Type == 10)
            {
                Flag = true;
            }
            else {
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