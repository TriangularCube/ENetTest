using ENet;
using UnityEngine;

namespace ENetTest{

	public class Player{
	    public uint playerID;
	    public bool isHost;
	    public Peer peer;

        // Host should always be 0
        private static uint nextID = 1;
	    public static uint GetNextID(){
	        if( !NetworkManager.instance.isHost ) {
	            Debug.LogError( "Cannot get next player ID from not host" );
	            return 0;
	        }

	        return nextID++;
	    }
	}
	
}
