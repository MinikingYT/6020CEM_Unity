using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Net.Http.Headers;
//using System.Diagnostics;

public class ConnectToServer : MonoBehaviour
{


    public struct UdpState
    {
        public UdpClient udpClient;
        public IPEndPoint ipEndpoint;
    }

    [SerializeField] GameObject networkAvatar;
    public List<NetworkGameObject> worldState;
    string receiveString = "";
    System.Diagnostics.Stopwatch pingTimer = new System.Diagnostics.Stopwatch();
    //static UdpClient client;
    //static IPEndPoint ep;
    static UdpState state;
    TimeSpan timer = new TimeSpan();
    public TextMeshProUGUI txt;
    //public List<NetworkGameObject> netObjects;
    string ipAdress = "25.13.185.212";
    // Start is called before the first frame update

//sends the information from all the local objects to the server, so they can be displayed properly to the other clients
    IEnumerator SendNetworkUpdates()
    {
        while (true)
        {
            worldState = new List<NetworkGameObject>();
            worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

            foreach (NetworkGameObject netObject in worldState)
            {
                if (netObject.isLocallyOwned && netObject.uniqueNetworkID != 0)
                {
                    state.udpClient.Send(netObject.ToPacket(), netObject.ToPacket().Length);
                }
            }
    
//happens every 0.5 seconds
            yield return new WaitForSeconds(0.5f);
        }
    }


//gets the packets from the server
    IEnumerator updateWorldState()
    {
        while (true)
        {
            //read in the current world state as all network game objects in the scene
            worldState = new List<NetworkGameObject>();
            worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());
            
            //cache the recieved packet string - we'll use that later to suspend the couroutine until it changes
            string previousRecieveString = receiveString;

            //if it's an object update, process it, otherwise skip
            if (previousRecieveString.Contains("Object data;"))
            {
                //we'll want to know if an object with this global id is already in the game world
                bool objectIsAlreadyInWorld = false;

                //we'll also want to exclude any invalid packets with a bad global id
                if (GetGlobalIDFromPacket(previousRecieveString) != 0)
                {
                    //for every networked gameobject in the world
                    foreach (NetworkGameObject ngo in worldState)
                    {
                        //if it's unique ID matches the packet, update it's position from the packet
                        if (ngo.uniqueNetworkID == GetGlobalIDFromPacket(previousRecieveString))
                        {
                            //only update it if we don't own it - you might want to try disabling and seeing the effect
                            if (!ngo.isLocallyOwned)
                            {
                                ngo.FromPacket(previousRecieveString); 

                            }
                            //if we have any uniqueID matches, our object is in the world
                            objectIsAlreadyInWorld = true;
                        }

                    }

                    //if it's not in the world, we need to spawn it
                    if (!objectIsAlreadyInWorld)
                    {
                        GameObject otherPlayerAvatar = Instantiate(networkAvatar);
                        //update its component properties from the packet
                        otherPlayerAvatar.GetComponent<NetworkGameObject>().uniqueNetworkID = GetGlobalIDFromPacket(previousRecieveString);
                        otherPlayerAvatar.GetComponent<NetworkGameObject>().FromPacket(previousRecieveString);
                        
                    }
                }

            }
            else if (receiveString.Contains("HealthUpdate:")){ //damage the other clients
                //get the damage value sent by the server

                Debug.Log("got here");

                float updatedHealth = float.Parse(betweenStrings(receiveString, "HealthUpdate:", " "));
                Debug.Log(updatedHealth);
                NetworkGameObject targetNGO = null;
                foreach (NetworkGameObject ngo in worldState)
                    {
                        if (ngo.uniqueNetworkID == GetGlobalIDFromPacket(receiveString))
                        {
                             targetNGO = ngo;
                            break;
                        }

                         if (targetNGO != null)
                        {
                                targetNGO.SetHP(updatedHealth);
                        }
                } 
            }
            //wait until the incoming string with packet data changes then iterate again
            if(state.udpClient.Available == 0)
                yield return new WaitForEndOfFrame();
            //yield return new WaitUntil(()=>!receiveString.Equals(previousRecieveString));
        }
    }

    
//only runs once on the start
    void Start()
    {
        
        Debug.Log("starting up");

//creates a new client with the given ip address
        state.udpClient = new UdpClient();
        state.ipEndpoint = new IPEndPoint(IPAddress.Parse(ipAdress), 9050); // endpoint where server is listening (testing localy)
        state.udpClient.Connect(state.ipEndpoint);
       
        RequestUIDs();

        
        string myMessage = "FirstEntrance";
        byte[] array = Encoding.ASCII.GetBytes(myMessage);
        //Sends the message which will give us a unique id
        state.udpClient.Send(array, array.Length);
        //Starts recieving from the server
        state.udpClient.BeginReceive(ReceiveAsyncCallback, state);

        void ReceiveAsyncCallback(IAsyncResult result)
        {
            byte[] receiveBytes = state.udpClient.EndReceive(result, ref state.ipEndpoint); //get the packet
            receiveString = Encoding.ASCII.GetString(receiveBytes); //decode the packet
            //Debug.Log("Received " + receiveString + " from " + state.ipEndpoint.ToString()); //display the packet                    
            assignUids(receiveBytes, receiveString);
            state.udpClient.BeginReceive(ReceiveAsyncCallback, state); //self-callback, meaning this loops infinitely

        }

        //start coroutines, that will be run while the code is running
        StartCoroutine(SendNetworkUpdates());
        StartCoroutine(updateWorldState());
        StartCoroutine(SendHeartbeat());
    }


//gets a global id sent by the server
    int GetGlobalIDFromPacket(String packet)
    {
        return Int32.Parse(packet.Split(';')[1]); 
    }

//assigns unique ids sent by the server to the objescts without them
    void assignUids(byte[] receiveBytes, string receiveString)
    {
        //Debug.Log(receiveString);
        if (receiveString.Contains("Assigned UID:"))
        {

           

            int parseFrom = receiveString.IndexOf(':');
            int parseTo = receiveString.LastIndexOf(';');

            //we need to parse the string from the server back into ints to work with
            int localID = Int32.Parse(betweenStrings(receiveString, ":", ";"));
            int globalID = Int32.Parse(receiveString.Substring(receiveString.IndexOf(";") + 1));

            Debug.Log("Got assignment: " + localID + " local to: " + globalID + " global");

            foreach (NetworkGameObject netObject in worldState)
            {
                //if the local ID sent by the server matches this game object
                if (netObject.localID == localID)
                {
                    Debug.Log(localID + " : " + globalID);
                    //the global ID becomes the server-provided value
                    netObject.uniqueNetworkID = globalID;
                    //Debug.Log("entered assignUids");
                }
            }
        }
    }


//can be called in other functions to add completly new functions to the server
    public void SendCustomMessage(string message)
    {
        byte[] messageBytes = Encoding.ASCII.GetBytes(message);
        state.udpClient.Send(messageBytes, messageBytes.Length);
    }


//asks for unique ids from the server
    void RequestUIDs()
    {

        worldState = new List<NetworkGameObject>();
        worldState.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

        //loops through all game objects in the network
        foreach (NetworkGameObject netObject in worldState)
        {
            //if they are local and dont yet have a unique id
            if (netObject.isLocallyOwned && netObject.uniqueNetworkID == 0)
            {
                //send message asking for an id to the server
                string myMessage = "I need a UID for local object:" + netObject.localID;
               // Debug.Log("SENT");
                byte[] array = Encoding.ASCII.GetBytes(myMessage);
                state.udpClient.Send(array, array.Length);
            }
        }
    }


//seends a heartbeat message to the server to show the player is still inside
    IEnumerator SendHeartbeat()
    {
        //while thee program is running (since script is never deleted)
        while (true)
        {
            string heartbeatMessage = "Heartbeat";
            byte[] array = Encoding.ASCII.GetBytes(heartbeatMessage);
            state.udpClient.Send(array, array.Length);

            //send the "heartbeat" message every 1 seconds
            yield return new WaitForSeconds(3);
        }
    }

    // Update is called once per frame
    void Update()
    {
       
    }


//checks the values in between 2 points of a string
    public static String betweenStrings(String text, String start, String end)
    {
        int p1 = text.IndexOf(start) + start.Length;
        int p2 = text.IndexOf(end, p1);

        if (end == "") return (text.Substring(p1));
        else return text.Substring(p1, p2 - p1);
    }


}
