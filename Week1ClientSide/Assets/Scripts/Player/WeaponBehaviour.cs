using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour
{

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

        connection = gameManager.gameObject.GetComponent<ConnectToServer>();
    }


    public void Shoot(){
         RaycastHit hit;
      
        int layerMask = 1 << 8;
        
        // This would cast rays only against colliders in layer 8.
        // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
        
        layerMask = ~layerMask;

        Vector3 cameraPosition = playerCamera.transform.position;
        Vector3 cameraForward = playerCamera.transform.forward;



        if (Physics.Raycast(cameraPosition, cameraForward, out hit, MaxShotDistance, layerMask))
        {
            Debug.DrawRay(cameraPosition, cameraForward * hit.distance, Color.yellow);
            Debug.Log("Did Hit");
            if(hit.collider.gameObject.name == "EnemyPlayer(Clone)"){
                Debug.Log("Hit A Player");
                NetworkGameObject gameObjRef =  hit.collider.gameObject.GetComponent<NetworkGameObject>();
                int globalID = gameObjRef.uniqueNetworkID;

                connection.SendCustomMessage("causeDamage: "+damage+ " ; "+ globalID);

                Debug.Log("causeDamage: "+damage+ " ; "+ globalID);
            }
             
        }
        else
        {
            Debug.DrawRay(cameraPosition, cameraForward * MaxShotDistance, Color.white);
            Debug.Log("Did not Hit");
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
