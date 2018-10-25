using System.Linq;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ENetTest{

	public class NetworkIDSet {

	    [PostProcessScene]
	    public static void ProcessNetworkIDs(){
	        uint currentID = 1;

	        var IDs = Object.FindObjectsOfType<NetworkObject>();

            var used = ( from obj in IDs
                         where obj.networkID != 0
                         select obj.networkID ).ToList();

	        foreach( var obj in IDs ) {
	            while( used.Contains( obj.networkID ) ) {
	                currentID++;
	            }

	            obj.networkID = currentID++;
	        }

	    }

	}
	
}
