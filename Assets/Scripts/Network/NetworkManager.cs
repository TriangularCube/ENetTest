using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ENet;
using LiteNetLib.Utils;
using EventType = ENet.EventType;

namespace ENetTest{

    [RequireComponent(typeof(NetworkGUI))]
	public class NetworkManager : MonoBehaviour{

        #region Singleton
        private static NetworkManager _instance;
        public static NetworkManager instance { get{ return _instance; } }

        private void Awake(){
            if( _instance != null ) {
                Debug.Log( "There is already an instance of NetworkManager!?" );
                Destroy( this );
                return;
            }
            _instance = this;

            DontDestroyOnLoad( this );
        }
        #endregion


        private Host host;
        private readonly System.Diagnostics.Stopwatch processWatch = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// The maximum amount of time to process network events for one frame
        /// </summary>
        [SerializeField] private int processTime = 25;

        /// <summary>
        /// Number of times sent per second for Sync
        /// </summary>
        [SerializeField] private float syncRate = 20;
        private float syncThreshold { get { return 1000f / syncRate; } }
        private float syncTimer = 0;


        public bool isHost = false;
        public Peer client;
        public Peer server;


        // Stolen from LiteNetLib
        public NetDataWriter writer = new NetDataWriter();
        public NetDataReader reader = new NetDataReader();


        private Dictionary<uint, NetworkObject> networkObjects = new Dictionary<uint, NetworkObject>();
        public uint LocalPlayer;

        /// <summary>
        /// The prefab that will spawn for each player
        /// </summary>
        [SerializeField] private GameObject playerPrefab;


        private List<Player> players = new List<Player>();
        public Player GetPlayerByID( uint id ){
            return players.Find( x => x.playerID == id );
        }


        public void RegisterNetworkObject( uint id, NetworkObject obj ){
            networkObjects.Add( id, obj );
        }


        public void StartServer(){

            Library.Initialize();
            
            host = new Host();

            Address address = new Address {
                Port = 23581// Arbitrary Port
            };

            host.Create( address, 3 );
            isHost = true;

            var newPlayer = new Player() {
                playerID = 0,
                isHost = true
            };

            players.Add( newPlayer );

            MakeNewPlayer( newPlayer );

            LocalPlayer = 0;

            Debug.Log( "Server Started" );

        }


        public void StartClient( string target ){

            Library.Initialize();

            host = new Host();

            Address address = new Address();

            if( !address.SetHost( target ) ) {
                Debug.Log( "Could not set Address" );
                return;
            }
            address.Port = 23581; // Arbitrary Port

            host.Create();

            server = host.Connect( address );

            Debug.Log( "Client Started" );

        }


        private void Update(){

            if( host == null || !host.IsSet ) return;

            // Process events
            processWatch.Start();
            ENet.Event netEvent;

            while( processWatch.ElapsedMilliseconds < processTime && host.Service( 0, out netEvent ) > 0 ) {

                switch( netEvent.Type ) {
                    case EventType.None:
                        Debug.Log( "No Event" );
                        break;
                    case EventType.Connect:

                        if( isHost ) {

                            var newPlayerID = Player.GetNextID();

                            // Send an update of everybody in the game back to the new player
                            writer.Reset();

                            writer.Put( CallbackID.PLAYERS_IN_SERVER_UPDATE );

                            writer.Put( players.Count );

                            foreach( var player in players ) {

                                writer.Put( player.playerID );
                                writer.Put( player.isHost );

                            }

                            // Send the assigned player id to the new connectee
                            writer.Put( newPlayerID );

                            var data = (byte[]) writer.Data.Clone();
                            Packet packet = new Packet();
                            packet.Create( data, PacketFlags.Reliable );

                            netEvent.Peer.Send( 0, ref packet );


                            // Now send an update of all objects in game already created
                            writer.Reset();

                            writer.Put( CallbackID.OBJECTS_IN_SERVER_UPDATE );

                            writer.Put( networkObjects.Count );

                            foreach( var obj in networkObjects ) {

                                writer.Put( obj.Value.networkID );
                                writer.Put( obj.Value.owner.playerID );
                            }

                            data = (byte[]) writer.Data.Clone();
                            packet.Create( data, PacketFlags.Reliable );

                            netEvent.Peer.Send( 0, ref packet );


                            // Add new player
                            var newPlayer = new Player{
                                isHost = false,
                                playerID = newPlayerID,
                                peer = netEvent.Peer
                            };

                            players.Add( newPlayer );

                            var newNetID = NetworkObject.GetNextNetworkID();

                            MakePlayer( newPlayer, newNetID );

                            // Send to all players informing of the new player join
                            writer.Reset();

                            writer.Put( CallbackID.NEW_PLAYER_JOIN );

                            writer.Put( newPlayer.playerID );
                            writer.Put( newNetID );

                            data = (byte[]) writer.Data.Clone();
                            packet.Create( data );

                            foreach( var player in players ) {
                                if( !player.isHost ) {
                                    player.peer.Send( 0, ref packet );
                                }
                            }

                        }

                        break;
                    case EventType.Disconnect:
                        Debug.Log( "Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP );
                        break;
                    case EventType.Timeout:
                        Debug.Log( "Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP );
                        break;
                    case EventType.Receive:

                        byte[] buffer = new byte[netEvent.Packet.Length];
                        netEvent.Packet.CopyTo( buffer );

                        reader.Clear();
                        reader.SetSource( buffer );

                        var id = reader.GetUInt();

                        switch( id ) {
                            case CallbackID.PLAYERS_IN_SERVER_UPDATE:
                                int mx = reader.GetInt();

                                for( int i = 0; i < mx; i++ ) {

                                    var newPlayer = new Player {
                                        playerID = reader.GetUInt(),
                                        isHost = reader.GetBool(),
                                        peer = netEvent.Peer
                                    };

                                    players.Add( newPlayer );

                                    //Debug.Log( "Player Count " + players.Count );

                                }

                                LocalPlayer = reader.GetUInt();
                                //Debug.Log( "Local player is " + LocalPlayer );
                                break;
                            case CallbackID.OBJECTS_IN_SERVER_UPDATE:

                                int max = reader.GetInt();

                                for( int i = 0; i < max; i++ ) {
                                    var netID = reader.GetUInt();
                                    var ownerID = reader.GetUInt();

                                    MakePlayer( GetPlayerByID( ownerID ), netID );
                                }

 
                                break;
                            case CallbackID.NEW_PLAYER_JOIN:

                                var playerID = reader.GetUInt();
                                var NID = reader.GetUInt();

                                var newP = new Player {
                                    playerID = playerID,
                                    isHost = false,
                                    peer = netEvent.Peer
                                };

                                players.Add( newP );

                                MakePlayer( newP, NID );

                                break;
                            case CallbackID.SYNC_UPDATE:

                                int cnt = reader.GetInt();

                                for( int i = 0; i < cnt; i++ ) {
                                    uint netid = reader.GetUInt();
                                    //Debug.Log( "Finding Net ID: " + netid );

                                    var ob = networkObjects[netid];

                                    //Debug.Log( reader.GetFloat() + " " + reader.GetFloat() + " " + reader.GetFloat() + " " + reader.GetFloat() + " " + reader.GetFloat() + " " + reader.GetFloat() + " " + reader.GetFloat() );

                                    ob.HandleSyncUpdate( reader );
                                }


                                break;
                            case CallbackID.SCENE_UPDATE:
                                if( networkObjects.ContainsKey( id ) ) {
                                    networkObjects[id].ProcessCallback( reader );
                                }
                                break;
                            default:
                                Debug.LogError( "Received a packet for " + id + " but have no registered object or callback handler with that ID" );
                                break;
                        }
                            


                        break;
                            
                }

                netEvent.Packet.Dispose();

            }

            processWatch.Reset();

            #region Sync

            syncTimer += Time.deltaTime * 1000;

            if( syncTimer > syncThreshold ) {

                writer.Reset();
                writer.Put( CallbackID.SYNC_UPDATE );

                List<TransformUpdate> updates = new List<TransformUpdate>();

                foreach( var obj in networkObjects ) {
                    var up = obj.Value.SyncUpdate();

                    if( up.update ) {
                        updates.Add( up );
                    }
                }

                writer.Put( updates.Count );

                foreach( var update in updates ) {
                    writer.Put( update.id );

                    writer.Put( update.px );
                    writer.Put( update.py );
                    writer.Put( update.pz );

                    writer.Put( update.rx );
                    writer.Put( update.ry );
                    writer.Put( update.rz );
                    writer.Put( update.rw );
                }

                var data = (byte[])writer.Data.Clone();
                Packet packet = new Packet();
                packet.Create( data, PacketFlags.Reliable );

                if( isHost ) {
                    foreach( var player in players ) {
                        if( !player.isHost ) {
                            player.peer.Send( 0, ref packet );
                        }
                    }
                } else {
                    server.Send( 0, ref packet );
                }

                syncTimer = 0;

            }

            #endregion

            host.Flush();

        }

        private void MakeNewPlayer( Player pl ){
           MakePlayer( pl, NetworkObject.GetNextNetworkID() );
        }

        private void MakePlayer( Player pl, uint netID ){
            var p = Instantiate( playerPrefab );
            var no = p.GetComponent<NetworkObject>();

            no.networkID = netID;
            no.owner = pl;
        }

        private void OnApplicationQuite(){
            if( host != null && host.IsSet ) {
                host.Dispose();
            }

            Library.Deinitialize();
        }
		
	}
	
}
