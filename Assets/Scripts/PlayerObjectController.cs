using LiteNetLib.Utils;
using UnityEngine;

namespace ENetTest{

    [RequireComponent(typeof(NetworkObject))]
	public class PlayerObjectController : MonoBehaviour{

        private NetworkObject netObj;

        private void Start(){
            netObj = GetComponent<NetworkObject>();

            netObj.RegisterCallback( CallbackID.SYNC_UPDATE, HandleTransformUpdate );
        }

        private void HandleTransformUpdate( NetDataReader reader ){

            var vector = reader.GetFloatArray();
            var rot = reader.GetFloatArray();

            transform.position = new Vector3( vector[0], vector[1], vector[2] );
            transform.rotation = Quaternion.Euler( rot[0], rot[1], rot[2] );

        }

        private void Update(){

            if( !netObj.isOwner ) return;

            transform.Translate( new Vector3( Input.GetAxis( "Horizontal" ) * Time.deltaTime, Input.GetAxis( "Vertical" ) * Time.deltaTime, 0 ) );

        }

    }
	
}
