
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Collections;


namespace Multicast_test
{
	
	
	public class networking
	{
		private IPAddress ip;
		private int port;
		private Socket sender;
		private Socket receiver;
		private Thread receive_thread;
		private Stack receive_buffer;

		
		
		public networking(string address, int port_num)
		{
			receive_buffer = new Stack();
			try{
				
				ip = IPAddress.Parse(address);
				this.port = port_num;
				
				
				
				// -- sender -- 
				// create a socket that will send over UDP
				sender=new Socket(AddressFamily.InterNetwork,
				                    SocketType.Dgram, ProtocolType.Udp);
				
				// this is the "multicast room" in terms of op and port
				IPEndPoint ipep = new IPEndPoint(ip, port);
				
				// tell the sockets object to connect to the "room" ( nothing actually happens )
				sender.Connect(ipep);
				
				
				// -- receiver --
				// socket that will listen over UDP
				receiver=new Socket(AddressFamily.InterNetwork,
				                    SocketType.Dgram, ProtocolType.Udp);
				
				// create a "multicast room" that will listen for anything from anyone but only on the specified port
				IPEndPoint ipep_any = new IPEndPoint(IPAddress.Any, port);
				
				// allow us to listen to sockets being used for transmission (local loopback)
				receiver.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				
				// tell the multicast object to bind to the "multicast room" 
				receiver.Bind(ipep_any);
				
				// receive all multicast packets (TTL is how many routers the packet can go through)
				receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 0); 
				
				// tell the "multicast room" that you're listening for anything
				receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip));
				
				

				
				}catch(System.Net.Sockets.SocketException e) {
					// this will spit out errors if there's a problem with any of the previous code
					Console.Error.WriteLine(e.Message);
					Console.Error.WriteLine(e.GetBaseException());
					Console.Error.WriteLine(e.ErrorCode);
			}
		}
		public void start_receiving(){
			// start the receive thread right away!
			
			receive_thread = new Thread(new ThreadStart(receive));
			receive_thread.IsBackground = true;
			
			receive_thread.Start();
		}
		
		public void stop_receiving(){
			
			receive_thread.Abort();
		}
		
		
		
		public void send(byte[] b)
		{
			// TODO: Create a feedback loop-thing that can slow down transmission rates
			
			try{
				// send the char array "b" without any special socket flags
				sender.Send(b,b.Length,SocketFlags.None);
				
				Console.WriteLine("Message sent");
				
			}catch(System.Net.Sockets.SocketException e) {
				// this will spit out errors if there's a problem with any of the previous code
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine(e.GetBaseException());
				Console.Error.WriteLine(e.ErrorCode);
			}
		}
		
		public void send_file_piece(FilePiece piece){
			send( piece.get_packet());
		}
		
		public byte[] PopReceiveBuffer(){
			lock(receive_buffer.SyncRoot){
				if (receive_buffer.Count > 0){
					return (byte[])receive_buffer.Pop();
				}else{
					return (byte[])null;
				}
			}
		}
		
		
		public bool GetReceiveStatus(){
			return receive_thread.IsAlive;
		}
		
		private void receive()
		{
			try{
				while(true) { 
					try{
						byte[] b=new byte[FilePiece.data_size + FilePiece.header_size];
						// receive up to FilePiece bytes
						receiver.Receive(b);
						lock(receive_buffer.SyncRoot)
							receive_buffer.Push( b);
					}catch{
						Console.WriteLine("Bad packet received. Sender should slow down a bit?");
					}
				}
			}catch(System.Net.Sockets.SocketException e) {
				Console.Error.WriteLine(e.Message);
				Console.Error.WriteLine(e.GetBaseException());
				Console.Error.WriteLine(e.ErrorCode);
			}
		}
	}
}
