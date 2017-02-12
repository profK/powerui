//--------------------------------------
//               PowerUI
//
//        For documentation or 
//    if you have any issues, visit
//        powerUI.kulestar.com
//
//    Copyright � 2013 Kulestar Ltd
//          www.kulestar.com
//--------------------------------------

using System;
using UnityEngine;
using PowerUI;


namespace PowerUI{
	
	/// <summary>
	/// PowerUI creates one of these automatically if it's needed.
	/// It causes the Update routine to occur.
	/// </summary>
	
	public class StandardUpdater:MonoBehaviour{
		
		public void Update(){
			UI.InternalUpdate();
		}
		
		public void OnGUI(){
			
			Event current=Event.current;
			EventType type=current.type;
			
			if(type==EventType.Repaint || type==EventType.Layout){
				return;
			}
			
			if(type==EventType.KeyUp){
				
				// Key up:
				PowerUI.Input.OnKeyPress(false,current.character,(int)current.keyCode);
				
			}else if(type==EventType.KeyDown){
				
				// Key down:
				PowerUI.Input.OnKeyPress(true,current.character,(int)current.keyCode);
				
			}else if(Input.SystemMouse!=null){
				
				// Look out for mouse events:
				if(type==EventType.MouseUp){
					
					// Release it:
					PowerUI.Input.SystemMouse.Release(current.button);
					
				}else if(type==EventType.MouseDown){
					
					// Press it down:
					PowerUI.Input.SystemMouse.Click(current.button);
					
				}else if(type==EventType.ScrollWheel){
					
					// Trigger the scrollwheel event:
					PowerUI.Input.OnScrollWheel(current.delta);
					
				}
				
			}
			
		}
		
		public void OnDisable(){
			// Called when a scene changes.
			UI.Destroy();
		}
		
		public void OnApplicationQuit(){
			
			// Run OnBeforeUnload, if an event is still attached:
			if(UI.document!=null){
				
				// Run onbeforeunload (always trusted):
				UI.document.window.dispatchEvent(new BeforeUnloadEvent());
				
			}
			
			// Make sure all timers are halted:
			UITimer.OnUnload(null);
			
		}
		
	}
	
}