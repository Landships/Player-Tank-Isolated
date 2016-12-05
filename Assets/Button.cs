namespace VRTK
{
    using UnityEngine;

    public class Button : VRTK_InteractableObject
    {
		public Cannon_Fire_CS cannon;
        public float downForce;
        public override void StartUsing(GameObject usingObject)
        {
            base.StartUsing(usingObject);
            Debug.Log("pushed");
            Debug.Log(this.gameObject);
            this.gameObject.GetComponent<Rigidbody>().AddForce(new Vector3(downForce, 0f, 0f), ForceMode.VelocityChange);
			//netcode stuff for firing
        }

        protected override void Start()
        {
            base.Start();
        }
    }
}
