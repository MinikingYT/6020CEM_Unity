using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Unity.UI;
using UnityEngine.UI;
using TMPro;
using System.Net.Http.Headers;
//using System.Diagnostics;

public class ConnectToServer : MonoBehaviour
{



    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    System.Diagnostics.Stopwatch pingTimer = new System.Diagnostics.Stopwatch();
    static UdpClient client;
    static IPEndPoint ep;
    static UdpState state;
    TimeSpan timer = new TimeSpan();
    public TextMeshProUGUI txt;
    public List<NetworkGameObject> netObjects;

    //string ipAdress = "127.0.0.1";
    //string ipAdress = "10.1.42.129";
    string ipAdress = "10.1.229.232";
    // Start is called before the first frame update


    IEnumerator SendNetworkUpdates()
    {
        while (true)
        {
            List<NetworkGameObject> netObjects = new List<NetworkGameObject>();
            netObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());

            foreach (NetworkGameObject netObject in netObjects)
            {
                if (netObject.isLocallyOwned)
                {
                    client.Send(netObject.ToPacket(), netObject.ToPacket().Length);
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }


    void Start()
    {
        
        Debug.Log("starting up");


        client = new UdpClient();
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ipAdress), 9050); // endpoint where server is listening (testing localy)
        client.Connect(ep);
       
        RequestUIDs();
        StartCoroutine(SendNetworkUpdates());


        string myMessage = "FirstEntrance";
        byte[] array = Encoding.ASCII.GetBytes(myMessage);
        client.Send(array, array.Length);

        client.BeginReceive(ReceiveAsyncCallback, state);

        void ReceiveAsyncCallback(IAsyncResult result)
        {

            byte[] receiveBytes = client.EndReceive(result, ref ep); //get the packet
            string receiveString = Encoding.ASCII.GetString(receiveBytes); //decode the packet
            Debug.Log("Received " + receiveString + " from " + ep.ToString()); //display the packet
            client.BeginReceive(ReceiveAsyncCallback, state); //self-callback, meaning this loops infinitely
            pingTimer.Stop();
            timer = pingTimer.Elapsed;
            string myMessage2 = "Im A Unity Client";
            byte[] array2 = Encoding.ASCII.GetBytes(myMessage2);
            client.Send(array2, array2.Length);
            pingTimer.Restart();
            pingTimer.Start();
            assignUids(receiveBytes, receiveString);

        }
        
    }


    void assignUids(byte[] receiveBytes, string receiveString)
    {

        if (receiveString.Contains("Assigned UID:"))
        {

            int parseFrom = receiveString.IndexOf(':');
            int parseTo = receiveString.LastIndexOf(';');

            //we need to parse the string from the server back into ints to work with
            int localID = Int32.Parse(betweenStrings(receiveString, ":", ";"));
            int globalID = Int32.Parse(receiveString.Substring(receiveString.IndexOf(";") + 1));

            Debug.Log("Got assignment: " + localID + " local to: " + globalID + " global");

            foreach (NetworkGameObject netObject in netObjects)
            {
                //if the local ID sent by the server matches this game object
                if (netObject.localID == localID)
                {
                    Debug.Log(localID + " : " + globalID);
                    //the global ID becomes the server-provided value
                    netObject.uniqueNetworkID = globalID;
                }
            }
        }
    }

    void RequestUIDs()
    {

        netObjects = new List<NetworkGameObject>();
        netObjects.AddRange(GameObject.FindObjectsOfType<NetworkGameObject>());
        foreach (NetworkGameObject netObject in netObjects)
        {
            if (netObject.isLocallyOwned && netObject.uniqueNetworkID == 0)
            {
                string myMessage = "I need a UID for local object:" + netObject.localID;
                byte[] array = Encoding.ASCII.GetBytes(myMessage);
                client.Send(array, array.Length);
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        //txt.text = "Ping: " + timer.Milliseconds;
    }


    public static String betweenStrings(String text, String start, String end)
    {
        int p1 = text.IndexOf(start) + start.Length;
        int p2 = text.IndexOf(end, p1);

        if (end == "") return (text.Substring(p1));
        else return text.Substring(p1, p2 - p1);
    }


}
