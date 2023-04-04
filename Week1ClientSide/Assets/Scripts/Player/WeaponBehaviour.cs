using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour
{

//variables
   public float damage;
    public Camera playerCamera;
    public GameObject player;
    private bool hasWeapon;
    [SerializeField] float MaxShotDistance = 1000.0f;

    [SerializeField] GameObject gameManager;

    private ConnectToServer connection;
    // Start is called before the first frame update
    void Start()
    {
        //on the start gets the script that allows connectio with the server

        connection = gameManager.gameObject.GetComponent<ConnectToServer>();
    }

//event to shoot a raycast
    public void Shoot(){
         RaycastHit hit;
      
        int layerMask = 1 << 8;
        
        //this only cast rays only against colliders that are not in layer 8.
        
        
        layerMask = ~layerMask;


        //raycast goes from camerapos forward
        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;



        if (Physics.Raycast(cameraPosition, cameraForward, out hit, MaxShotDistance, layerMask))
        {
            Debug.DrawRay(cameraPosition, cameraForward * hit.distance, Color.yellow);
         
         //if hits another client
            if(hit.collider.gameObject.name == "EnemyPlayer(Clone)"){
               

               //Send costume message with that client global id, and the damage he will take
                NetworkGameObject gameObjRef =  hit.collider.gameObject.GetComponent<NetworkGameObject>();
                int globalID = gameObjRef.uniqueNetworkID;

                connection.SendCustomMessage("causeDamage: "+damage+ " ; "+ globalID);

               
            }
             
        }
        else
        {
            Debug.DrawRay(cameraPosition, cameraForward * MaxShotDistance, Color.white);
           
        }
    }

     

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)){

            Shoot();
        }
    }
}
