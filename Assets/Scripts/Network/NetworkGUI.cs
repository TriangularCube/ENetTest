using System;
using UnityEngine;

namespace ENetTest{

	public class NetworkGUI : MonoBehaviour{

        private NetworkManager manager;

	    void Start(){
	        manager = GetComponent<NetworkManager>();
	    }

        private String ip = "localhost";

	    void OnGUI(){

	        if( GUI.Button( new Rect( 10, 10, 130, 30 ), "Start Server" ) ) {
	            manager.StartServer();
	        }

	        ip = GUI.TextField( new Rect( 10, 60, 200, 25 ), ip, 15 );

	        if( GUI.Button( new Rect( 10, 90, 130, 30 ), "Join IP" ) ) {
	            manager.StartClient( ip );
	        }

	    }
		
	}
	
}
