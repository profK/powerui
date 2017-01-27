//--------------------------------------
//               PowerUI
//
//        For documentation or 
//    if you have any issues, visit
//        powerUI.kulestar.com
//
//    Copyright © 2013 Kulestar Ltd
//          www.kulestar.com
//--------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Css;
using InfiniText;


namespace PowerUI{
	
	/// <summary>
	/// Stores the information used whilst laying out boxes during a reflow.
	/// <summary>

	public class LineBoxMeta{
		
		/// <summary>The "host" block box.</summary>
		public BlockBoxMeta HostBlock;
		/// <summary>The height of the current line being processed.</summary>
		public float LineHeight;
		/// <summary>A linked list of elements on a line are kept. This is the last element on the current line.</summary>
		internal LayoutBox LastOnLine;
		/// <summary>A linked list of elements on a line are kept. This is the first element on the current line.</summary>
		internal LayoutBox FirstOnLine;
		/// <summary>A linked list of elements on a line are kept. This is the last element on the current out of flow line.</summary>
		internal LayoutBox LastOutOfFlow;
		/// <summary>A linked list of elements on a line are kept. This is the first element on the current out of flow line.</summary>
		internal LayoutBox FirstOutOfFlow;
		/// <summary>The last line start. Tracked for alignment.</summary>
		internal LayoutBox LastLineStart;
		/// <summary>The first line start. Tracked for alignment.</summary>
		internal LayoutBox FirstLineStart;
		/// <summary>This boxes whitespace mode.</summary>
		internal int WhiteSpace;
		/// <summary>The current 'clear zone'. Added to PenY when something is added to the current line.</summary>
		internal float ClearY_;
		/// <summary>The set of active floated elements for the current line being rendered.</summary>
		internal FloatingElements Floats;
		/// <summary>The current x location of the renderer in screen pixels from the left.</summary>
		internal float PenX;
		/// <summary>The point at which lines begin at.</summary>
		public float LineStart;
		/// <summary>The value for vertical-align.</summary>
		public int VerticalAlign;
		/// <summary>Vertical-align offset from the baseline.</summary>
		public float VerticalAlignOffset;
		/// <summary>The current box being worked on.</summary>
		internal LayoutBox CurrentBox;
		/// <summary>The next box in the hierarchy.</summary>
		public LineBoxMeta Parent;
		/// <summary>The inline element.</summary>
		public RenderableData RenderData;
		/// <summary>An offset to apply to MaxX.</summary>
		public float MaxOffset;
		
		
		public LineBoxMeta(LineBoxMeta parent,LayoutBox firstBox,RenderableData renderData){
			
			Parent=parent;
			CurrentBox=firstBox;
			RenderData=renderData;
			
		}
		
		/// <summary>Removes the given box from this line. Must not be the first one.</summary>
		public void RemoveFromLine(LayoutBox box){
			
			if(FirstOnLine==null){
				return;
			}
			
			LayoutBox prev=null;
			LayoutBox current=FirstOnLine;
			
			while(current!=null){
				
				if(current==box){
					
					// Got it!
					if(prev==null){
						throw new Exception("Can't remove first box from a line.");
					}
					
					prev.NextOnLine=current.NextOnLine;
					
					if(current.NextOnLine==null){
						LastOnLine=prev;
					}
					
					break;
					
				}
				
				prev=current;
				current=current.NextOnLine;
				
			}
			
		}
		
		/// <summary>Attempts to break a line for a parent inline node.</summary>
		public void TryBreakParent(){
			
			// If the parent is an inline node, 'this' is the first element on its line.
			if(Parent==null){
				return;
			}
			
			if(Parent.FirstOnLine==CurrentBox){
				
				// Go another level up if we can:
				if(Parent is BlockBoxMeta){
					return;
				}
				
				Parent.TryBreakParent();
				return;
				
			}
			
			// We're not the first on the line (but we'll always be the last). Safety check:
			if(Parent.LastOnLine!=CurrentBox){
				return;
			}
			
			// Remove current box from the line:
			Parent.RemoveFromLine(CurrentBox);
			
			// Complete it:
			Parent.CompleteLine(true,true);
			
			// Re-add:
			Parent.AddToLine(CurrentBox);
			
			// Clear offset:
			MaxOffset=0f;
			
		}
		
		/// <summary>The value of the CSS line-height property.</summary>
		public float CssLineHeight;
		
		/// <summary>The current y location of the renderer in screen pixels from the top.</summary>
		public virtual float PenY{
			get{
				return 0f;
			}
			set{
			}
		}
		
		/// <summary>The length of the longest line so far. Used for the content width.</summary>
		public virtual float LargestLineWidth{
			get{
				return HostBlock.LargestLineWidth_;
			}
			set{
				HostBlock.LargestLineWidth_=value;
			}
		}
		
		/// <summary>True if the rendering direction is left. This originates from the direction: css property.</summary>
		public virtual bool GoingLeftwards{
			get{
				return HostBlock.GoingLeftwards_;
			}
			set{
				HostBlock.GoingLeftwards_=value;
			}
		}
		
		/// <summary>The x value that must not be exceeded by elements on a line. Used if the parent has fixed width.</summary>
		public virtual float MaxX{
			get{
				return HostBlock.MaxX_ - MaxOffset;
			}
			set{
				HostBlock.MaxX_=value;
			}
		}
		
		/// <summary>Ensures the given amount of space is available by 
		/// completing the line if needed and potentially clearing floats too.</summary>
		/// <returns>True if it broke.</returns>
		public int GetLineSpace(float width,float initialOffset){
			
			// If the box no longer fits on the current line..
			float space=(MaxX-PenX);
			
			if((initialOffset+width) <= space){
				return 0;
			}
			
			// Does it fit on a newline?
			if(width <= space){
				return 1;
			}
			
			// Still nope! If we've got any floats, try clearing them:
			while(TryClearFloat()){
				
				if(width <= (MaxX-PenX)){
					
					// Great, it'll fit on a newline!
					return 1;
					
				}
				
			}
			
			// Ok! This one's special because it doesn't actually fit on the line.
			// Important for words which may need to break internally.
			return 2;
			
		}
		
		/// <summary>Adds the given style to the current line.</summary>
		/// <param name="style">The style to add.</param>
		internal void AddToLine(LayoutBox styleBox){
			
			// Make sure it's safe:
			styleBox.Parent=CurrentBox;
			styleBox.NextLineStart=null;
			styleBox.NextOnLine=null;
			
			if((styleBox.PositionMode & PositionMode.InFlow)==0){
				
				// Out of flow - add it to a special line:
				if(FirstOutOfFlow==null){
					FirstOutOfFlow=LastOutOfFlow=styleBox;
				}else{
					LastOutOfFlow=LastOutOfFlow.NextOnLine=styleBox;
				}
				
				styleBox.ParentOffsetLeft=PenX+styleBox.Margin.Left;
				styleBox.ParentOffsetTop=PenY+styleBox.Margin.Top;
				
				return;
			}
			
			int floatMode=styleBox.FloatMode;
			
			if(floatMode==FloatMode.None){
				
				// In flow - add to line:
				
				// Add the clear zone:
				if(ClearY_!=0f){
					PenY+=ClearY_;
					ClearY_=0f;
				}
				
				if(FirstOnLine==null){
					FirstOnLine=styleBox;
					LastOnLine=styleBox;
					
					if(FirstLineStart==null){
						
						// First child element. Update parent if we've got one:
						if(Parent!=null && Parent.CurrentBox!=null){
							
							Parent.CurrentBox.FirstChild=styleBox;
							
						}
						
						FirstLineStart=LastLineStart=styleBox;
					}else{
						LastLineStart=LastLineStart.NextLineStart=styleBox;
					}
					
				}else{
					LastOnLine.NextOnLine=styleBox;
					LastOnLine=styleBox;
				}
				
			}else{
				
				// Adding a float - is this an inline element?
				if(this is InlineBoxMeta){
					
					// Add to nearest block:
					HostBlock.AddToLine(styleBox);
					return;
					
				}
				
				// Going left?
				if(GoingLeftwards){
					// Invert:
					floatMode=(floatMode==FloatMode.Right)?FloatMode.Left : FloatMode.Right;
				}
				
				if(Floats==null){
					Floats=new FloatingElements();
				}
				
				if(floatMode==FloatMode.Right){
					
					// Push down onto the FR stack:
					styleBox.NextOnLine=Floats.Right;
					Floats.Right=styleBox;
					
				}else{
					
					// Push down onto the FL stack:
					styleBox.NextOnLine=Floats.Left;
					Floats.Left=styleBox;
					
				}
				
			}
			
		}
		
		/// <summary>The float 'clearance' on the left/right. It's basically the bottom of left/right floats.</summary>
		public float FloatClearance(bool left){
			
			if(Floats==null){
				return 0f;
			}
			
			LayoutBox activeFloat=left ? Floats.Left : Floats.Right;
			
			float max=0f;
			
			while(activeFloat!=null){
				
				// Yes - how far down must we go?
				float requiredClear=(activeFloat.ParentOffsetTop + activeFloat.Height);
				
				if(requiredClear>max){
					max=requiredClear;
				}
				
				// Go left:
				activeFloat=activeFloat.NextOnLine;
				
			}
			
			return max;
			
		}
		
		/// <summary>Tries to clear a left/right float (whichever is shortest first).</summary>
		/// <returns>True if either side was cleared.</returns>
		public bool TryClearFloat(){
			
			if(Floats==null){
				return false;
			}
			
			if(Floats.Left==null){
				
				if(Floats.Right==null){
					return false;
				}
				
				// Clear right:
				ClearFloat(FloatMode.Right);
				
				return true;
				
			}
			
			if(Floats.Right==null){
				
				// Clear left:
				ClearFloat(FloatMode.Left);
				
				return true;
				
			}
			
			// Clear shortest:
			float clearanceL=FloatClearance(true);
			float clearanceR=FloatClearance(false);
			
			if(clearanceL>clearanceR){
				
				// R first.
				ClearFloat(FloatMode.Right);
				
			}else{
				
				// L first.
				ClearFloat(FloatMode.Left);
				
			}
			
			return true;
			
		}
		
		/// <summary>Clears left/right/both floats.</summary>
		public void ClearFloat(int mode){
			
			if(Floats==null){
				return;
			}
			
			LayoutBox activeFloat;
			float penY=PenY;
			
			if((mode & FloatMode.Left)!=0){
				
				// Clear left.
				activeFloat=Floats.Left;
				Floats.Left=null;
				
				while(activeFloat!=null){
					
					// Yes - how far down must we go?
					float requiredClear=(activeFloat.ParentOffsetTop + activeFloat.Height);
					
					if((penY+ClearY_)<requiredClear){
						// Clear over it now:
						ClearY_=requiredClear-penY;
					}
					
					// Decrease LineStart:
					LineStart-=activeFloat.TotalWidth;
					
					// Go left:
					activeFloat=activeFloat.NextOnLine;
					
				}
				
			}
			
			if((mode & FloatMode.Right)!=0){
				
				// Clear right.
				activeFloat=Floats.Right;
				Floats.Right=null;
				
				while(activeFloat!=null){
					
					// Yes - how far down must we go?
					float requiredClear=(activeFloat.ParentOffsetTop + activeFloat.Height);
					
					if((penY+ClearY_)<requiredClear){
						// Clear over it now:
						ClearY_=requiredClear-penY;
					}
					
					// Increase max x:
					MaxX+=activeFloat.TotalWidth;
					
					// Get the next float:right:
					LayoutBox next=activeFloat.NextOnLine;
					
					// Go right:
					activeFloat=next;
					
				}
				
			}
			
		}
		
		/// <summary>Completes a line, optionally breaking it.</summary>
		public void CompleteLine(bool breakLine,bool topOfStack){
			
			float lineHeight=LineHeight;
			
			if(breakLine || topOfStack){
				
				// Vertically align all elements on the current line and reset it:
				LayoutBox currentBox=FirstOnLine;
				LayoutBox first=currentBox;
				FirstOnLine=null;
				LastOnLine=null;
				
				// Baseline is default:
				int verticalAlignMode=VerticalAlign;
				float baseOffset=VerticalAlignOffset;
				
				while(currentBox!=null){
					// Calculate the offset to where the top left corner is (of the complete box, margin included):
					
					// Must be positioned such that the boxes sit on this lines baseline.
					// the baseline is by default at half the line-height but moves up whenever 
					// an inline-block element with padding/border/margin is added.
					
					float delta=-(currentBox.Height+currentBox.Margin.Bottom);
					
					bool inline=(currentBox.DisplayMode & DisplayMode.OutsideInline)!=0;
					
					if(currentBox.DisplayMode==DisplayMode.Inline){
						
						// Must also move it down by padding and border:
						delta+=currentBox.Border.Bottom + currentBox.Padding.Bottom;
						
					}
					
					switch(verticalAlignMode){
						
						case VerticalAlignMode.Baseline:
							
							if(inline){
								
								// Bump the elements so they all sit neatly on the baseline:
								float baselineShift=(CurrentBox.Baseline-currentBox.Baseline)+baseOffset;
								delta-=baselineShift;
								
								// May need to update the line height:
								
								if(baselineShift>0){
									
									// (This is where gaps come from below inline images):
									
									if(currentBox.DisplayMode==DisplayMode.Inline){
										
										// Line height next:
										baselineShift+=currentBox.InnerHeight;
										
									}else{
										
										// E.g. inline-block:
										baselineShift+=currentBox.TotalHeight;
									}
									
									if(baselineShift>LineHeight){
										
										LineHeight=baselineShift;
										lineHeight=baselineShift;
										
										// Stalled!
										
										// - This happens because we've just found out that an element sitting on the baseline
										//   has generated a gap and ended up making the line get taller.
										//   Elements after this one can affect the baseline so we can't "pre test" this condition.
										//   Line height is important for positioning elements, so we'll need to go again
										//   on the elements that we've already vertically aligned.
										
										// Halt and try again:
										currentBox=first;
										goto Stall;
										
									}
									
								}
								
							}
							
						break;
						
					}
					
					currentBox.ParentOffsetTop=PenY+delta+lineHeight;
					
					// Hop to the next one:
					currentBox=currentBox.NextOnLine;
					
					Stall:
						continue;
					
				}
				
				currentBox=FirstOutOfFlow;
				FirstOutOfFlow=null;
				LastOutOfFlow=null;
				
				while(currentBox!=null){
					// Calculate the offset to where the top left corner is (of the complete box, margin included):
					
					// Just margin for these ones:
					float delta=-(currentBox.Margin.Bottom);
					
					if((currentBox.DisplayMode & DisplayMode.OutsideInline)!=0){
						
						// Must also move it down by padding and border:
						delta+=currentBox.Border.Bottom + currentBox.Padding.Bottom;
						
					}else if((currentBox.DisplayMode & DisplayMode.OutsideBlock)!=0){
						
						// Clear x:
						currentBox.ParentOffsetLeft=LineStart;
						
					}
					
					currentBox.ParentOffsetTop=PenY+delta+lineHeight;
					
					// Hop to the next one:
					currentBox=currentBox.NextOnLine;
				}
				
			}
			
			// Recurse down to the nearest flow root element.
			
			if(this is InlineBoxMeta){
				
				// Apply valid width/height:
				LayoutBox box=CurrentBox;
				
				bool inFlow=((box.PositionMode & PositionMode.InFlow)!=0);
				
				// Update line height and baseline:
				if(inFlow){
					
					if(lineHeight>Parent.LineHeight){
						Parent.LineHeight=lineHeight;
					}
					
					if(CurrentBox.Baseline>Parent.CurrentBox.Baseline){
						Parent.CurrentBox.Baseline=CurrentBox.Baseline;
					}
					
				}
				
				// Otherwise it explicitly defined them ("inline replaced").
				if(box.OrdinaryInline){
					
					box.InnerHeight=lineHeight;
					box.InnerWidth=PenX-LineStart;
					box.SetDimensions(false,false);
					
					// Update content w/h:
					box.ContentHeight=box.InnerHeight;
					box.ContentWidth=box.InnerWidth;
					
				}
				
				if(inFlow){
					// Update dim's:
					Parent.AdvancePen(box);
				}
				
				if(inFlow && breakLine){
					
					// Linebreak the parent:
					Parent.CompleteLine(breakLine,false);
					
					// Create a new box!
					// (And add it to the parent)
					LayoutBox styleBox=new LayoutBox();
					styleBox.Border=box.Border;
					styleBox.Padding=box.Padding;
					styleBox.Margin=box.Margin;
					styleBox.NextInElement=null;
					
					// No left margin:
					styleBox.Margin.Left=0f;
					
					styleBox.DisplayMode=box.DisplayMode;
					styleBox.PositionMode=box.PositionMode;
					
					CurrentBox=styleBox;
					
					// Add to the inline element's render data:
					RenderData.LastBox.NextInElement=styleBox;
					RenderData.LastBox=styleBox;
					
					// Add to line next:
					Parent.AddToLine(styleBox);
					
				}
				
			}else{
				
				// Done recursing downwards - we're at the block!
				
				if(breakLine || topOfStack){
					
					// Update largest line (excludes float right which is actually what we want!):
					if(PenX>LargestLineWidth){
						LargestLineWidth=PenX;
					}
					
					// Move the pen down to the following line:
					PenY+=lineHeight;
					
					// Are any floats now cleared?
					if(Floats!=null){
						
						LayoutBox activeFloat=Floats.Left;
						
						while(activeFloat!=null){
							
							// Is the current render point now higher than this floating object?
							// If so, we must reduce LineStart/ increase MaxX depending on which type of float it is.
							
							if(PenY>=(activeFloat.ParentOffsetTop + activeFloat.Height)){
								
								// Clear!
								
								// Pop:
								Floats.Left=activeFloat.NextOnLine;
								
								// Decrease LineStart:
								LineStart-=activeFloat.TotalWidth;
								
							}else{
								
								// Didn't clear - stop there.
								// (We don't want to clear any further over to the left).
								break;
								
							}
							
							activeFloat=activeFloat.NextOnLine;
						}
						
						// Test clear right:
						activeFloat=Floats.Right;
						
						while(activeFloat!=null){
							
							// Is the current render point now higher than this floating object?
							// If so, we must reduce LineStart/ increase MaxX depending on which type of float it is.
							
							if(PenY>=(activeFloat.ParentOffsetTop + activeFloat.Height)){
								
								// Clear!
								
								// Pop:
								Floats.Right=activeFloat.NextOnLine;
								
								// Increase max x:
								MaxX+=activeFloat.TotalWidth;
								
							}else{
								
								// Didn't clear - stop there.
								// (We don't want to clear any further over to the right).
								break;
								
							}
							
							activeFloat=activeFloat.NextOnLine;
						}
						
					}
					
				}
				
			}
			
			if(breakLine){
				
				// Finally, reset the pen 
				// (this is after the recursion call, so we've cleared floats etc):
				MaxOffset=0f;
				PenX=LineStart;
				LineHeight=0f;
				
			}
			
		}
		
		/// <summary>Advances the pen now.</summary>
		public void AdvancePen(LayoutBox styleBox){
			
			int floatMode=styleBox.FloatMode;
			
			if(floatMode!=FloatMode.None && this is BlockBoxMeta){
				
				// Float (block/inline-block only):
				BlockBoxMeta bbm=this as BlockBoxMeta;
				
				float totalWidth=styleBox.TotalWidth;
				
				// What's the opposite side?
				int invertFloat=(floatMode==FloatMode.Right)?FloatMode.Left : FloatMode.Right;
				
				if(GoingLeftwards){
					// Going the other way - flip sides:
					int a=floatMode;
					floatMode=invertFloat;
					invertFloat=a;
				}
				
				if((bbm.MaxX_-totalWidth)<bbm.LineStart_){
					
					// Clear other side:
					ClearFloat(invertFloat);
					
				}
				
				// Always apply top here (no vertical-align and must be after the above clear):
				styleBox.ParentOffsetTop=bbm.PenY_ + bbm.ClearY_;
				
				if(floatMode==FloatMode.Left){
					
					styleBox.ParentOffsetLeft=LineStart+styleBox.Margin.Left;
					PenX+=totalWidth;
					
					// Push over where lines start at:
					bbm.LineStart_+=totalWidth;
					
					// If it's not the first on the line then..
					if(styleBox!=FirstOnLine){
						
						// Push over all the elements before this on the line.
						LayoutBox currentLine=FirstOnLine;
						
						while(currentLine!=styleBox && currentLine!=null){
							
							// Move it:
							currentLine.ParentOffsetLeft+=styleBox.Width;
							
							// Next one:
							currentLine=currentLine.NextOnLine;
							
						}
						
					}
					
				}else{
					
					// Special case for any inline-block which isn't in block mode.
					// MaxX is *not* the final position for it - we'll revisit it when we know how much of a 'gap' there is.
					styleBox.ParentOffsetLeft=bbm.MaxX_-totalWidth+styleBox.Margin.Left;
					
					if(styleBox.ParentOffsetLeft<0f){
						styleBox.ParentOffsetLeft=0f;
					}
					
					// Reduce max:
					bbm.MaxX_-=totalWidth;
					
				}
				
			}else if(GoingLeftwards){
				
				PenX+=styleBox.Width+styleBox.Margin.Right;
				styleBox.ParentOffsetLeft=LineStart*2-PenX;
				PenX+=styleBox.Margin.Left;
				
				// If it's not a flow root then don't use total height.
				// If it's a word then we don't check it at all.
				float effectiveHeight;
				
				if(styleBox.DisplayMode==DisplayMode.Inline){
					effectiveHeight=styleBox.InnerHeight;
				}else{
					effectiveHeight=styleBox.TotalHeight;
				}
				
				if(effectiveHeight>LineHeight){
					LineHeight=effectiveHeight;
				}
				
				float baseline=styleBox.Baseline;
				
				if(baseline>CurrentBox.Baseline){
					CurrentBox.Baseline=baseline;
				}
				
			}else{
				
				PenX+=styleBox.Margin.Left;
				styleBox.ParentOffsetLeft=PenX;
				PenX+=styleBox.Width+styleBox.Margin.Right;
				
				// If it's not a flow root then don't use total height.
				// If it's a word then we don't check it at all.
				float effectiveHeight;
				
				if(styleBox.DisplayMode==DisplayMode.Inline){
				
					effectiveHeight=styleBox.InnerHeight;
				}else{
					effectiveHeight=styleBox.TotalHeight;
				}
				
				if(effectiveHeight>LineHeight){
					LineHeight=effectiveHeight;
				}
				
				float baseline=styleBox.Baseline;
				
				if(baseline>CurrentBox.Baseline){
					CurrentBox.Baseline=baseline;
				}
				
			}
			
		}
		
	}
	
	public class BlockBoxMeta : LineBoxMeta{
		
		/// <summary>The current y location of the renderer in screen pixels from the top.</summary>
		internal float PenY_;
		/// <summary>The point at which lines begin at.</summary>
		internal float LineStart_;
		/// <summary>True if the rendering direction is left. This originates from the direction: css property.</summary>
		internal bool GoingLeftwards_;
		/// <summary>The x value that must not be exceeded by elements on a line. Used if the parent has fixed width.</summary>
		internal float MaxX_;
		/// <summary>The previous block margin (margin-bottom). Used for margin collapsing.</summary>
		public float PreviousMargin;
		/// <summary>The length of the longest line so far.</summary>
		public float LargestLineWidth_;
		
		
		public BlockBoxMeta(LineBoxMeta parent,LayoutBox firstBox,RenderableData renderData):base(parent,firstBox,renderData){}
		
		/// <summary>The current y location of the renderer in screen pixels from the top.</summary>
		public override float PenY{
			get{
				return PenY_;
			}
			set{
				PenY_=value;
			}
		}
		
		/// <summary>True if the rendering direction is left. This originates from the direction: css property.</summary>
		public override bool GoingLeftwards{
			get{
				return GoingLeftwards_;
			}
			set{
				GoingLeftwards_=value;
			}
		}
		
		/// <summary>The x value that must not be exceeded by elements on a line. Used if the parent has fixed width.</summary>
		public override float MaxX{
			get{
				return MaxX_;
			}
			set{
				MaxX_=value;
			}
		}
		
		/// <summary>The length of the longest line so far.</summary>
		public override float LargestLineWidth{
			get{
				return LargestLineWidth_;
			}
			set{
				LargestLineWidth_=value;
			}
		}
		
	}
	
	public class InlineBoxMeta : LineBoxMeta{
		
		public InlineBoxMeta(BlockBoxMeta block,LineBoxMeta parent,LayoutBox firstBox,RenderableData renderData):base(parent,firstBox,renderData){
			
			MaxOffset=parent.PenX + firstBox.InlineStyleOffsetLeft;
			HostBlock=block;
			
		}
		
	}
	
}