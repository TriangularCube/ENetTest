using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

namespace ENetTest{

    public struct TransformUpdate{
        public bool update;
        public uint id;
        public float px, py, pz;
        public float rx, ry, rz, rw;
    }

	public class NetworkObject : MonoBehaviour{

	    private static uint nextID = 400;
	    public static uint GetNextNetworkID(){
	        if( !NetworkManager.instance.isHost ) {
	            Debug.LogError( "Cannot ask for next Network ID from a client" );
	            return 0;
	        }
	        return nextID++;
	    }

	    public uint networkID = 0;

	    private Player _owner;
	    public Player owner{
            get{ return _owner; }
	        set{
	            // TODO Guard against client owner change
                Debug.Log( "Setting owner to " + value.playerID );
	            _owner = value;
	        }
	    }
        public bool isOwner { get{ return _owner != null && _owner.playerID == NetworkManager.instance.LocalPlayer; } }

	    private void Start(){
	        NetworkManager.instance.RegisterNetworkObject( networkID, this );
            RegisterCallback( CallbackID.SET_OWNER, HandleOwnerUpdate );
	    }


	    #region Sync

	    public TransformUpdate SyncUpdate(){

            var up = new TransformUpdate();

	        if( !isOwner ) {
	            up.update = false;
	            return up;
	        }

	        up.update = true;

	        up.id = networkID;

	        up.px = transform.position.x;
	        up.py = transform.position.y;
	        up.pz = transform.position.z;

	        up.rx = transform.rotation.x;
	        up.ry = transform.rotation.y;
	        up.rz = transform.rotation.z;
	        up.rw = transform.rotation.w;

	        return up;

	    }

	    public void HandleSyncUpdate( NetDataReader reader ){
	        
            var position = new Vector3( reader.GetFloat(), reader.GetFloat(), reader.GetFloat() );
            var rotation = new Quaternion( reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat() );

	        transform.position = Vector3.Lerp( transform.position, position, 0.1f );
	        transform.rotation = Quaternion.Slerp( transform.rotation, rotation, 0.1f );

	    }

	    #endregion



        #region Callback

        private readonly Dictionary<uint, Action<NetDataReader>> callbacks = new Dictionary<uint, Action<NetDataReader>>();

	    public void RegisterCallback( uint id, Action<NetDataReader> callback ) {
	        callbacks.Add( id, callback );
	    }

	    public void ProcessCallback( NetDataReader reader ) {
	        var id = reader.GetUInt();

	        if( callbacks.ContainsKey( id ) ) {
	            callbacks[id]( reader );
	        } else {
	            Debug.Log( "Network Object " + networkID + " does not contain a definition for Action " + id );
	        }
	    }

	    private void HandleOwnerUpdate( NetDataReader reader ){
	        var ownerID = reader.GetUInt();

	        _owner = NetworkManager.instance.GetPlayerByID( ownerID );
	    }

        #endregion


    }
	
}
