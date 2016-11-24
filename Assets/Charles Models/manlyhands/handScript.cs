using UnityEngine;
using System.Collections;

public class handScript : MonoBehaviour {
    private Animator handAnimator;
    private float currentBlend = 0.0f;
    //public SteamVR_Controller.Device VR_Controller_Script;
    private bool canGrab = false;
    private int deviceIndex = -1;

    // Use this for initialization
    void Start () {
        //Debug.Log("handscript starting");
        handAnimator = GetComponent<Animator>();
        /*
        if (gameObject.transform.name == "left hand")
        {
            Debug.Log("is left hand");
            deviceIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.FarthestRight);
        }
        else if (gameObject.transform.name == "right hand")
        {
            Debug.Log("is right hand");

            deviceIndex = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.FarthestLeft);
        }
        */

    }

    // Update is called once per frame
    void Update () {
        //Debug.Log("device index: " + deviceIndex);
        /*
        if (deviceIndex != -1 && SteamVR_Controller.Input(deviceIndex).GetPress(SteamVR_Controller.ButtonMask.Trigger))
        {
            //Debug.Log("is pulling trigger on hand");
            handAnimator.SetFloat("handBlend", 1.0f, 0.1f, Time.deltaTime);
        }
        else if (transform.parent.GetComponent<InteractPromptScript>().canGrab)
        {
            handAnimator.SetFloat("handBlend", 0.5f, 0.1f, Time.deltaTime);
        }
        else
        {
            handAnimator.SetFloat("handBlend", 0.0f, 0.1f, Time.deltaTime);
        }*/

        if (gameObject.transform.parent.GetComponent<PseudoHand>().trigger_on)
        {
            handAnimator.SetFloat("handBlend", 1.0f, 0.1f, Time.deltaTime);
        }
        else if (transform.parent.GetComponent<InteractPromptScript>().canGrab)
        {
            handAnimator.SetFloat("handBlend", 0.5f, 0.1f, Time.deltaTime);
        }
        else
        {
            handAnimator.SetFloat("handBlend", 0.0f, 0.1f, Time.deltaTime);
        }

    }




    public void hapticFeedBack()
    {
        SteamVR_Controller.Input(deviceIndex).TriggerHapticPulse(250);
    }
}
