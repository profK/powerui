using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JavaScript
{
	/// <summary>
	/// A Uint8 array. Essentially just a byte[].
	/// </summary>
	public class Uint8ClampedArray : TypedArray
	{
		
		/// <summary>A reference to the internal buffer.</summary>
		private byte[] buffer;
		
		// new Uint8ClampedArray(length);
		// new Uint8ClampedArray(typedArray);
		// new Uint8ClampedArray(object);
		// new Uint8ClampedArray(buffer[, byteOffset[, length]]);
		
		public Uint8ClampedArray(int length):base(TypedArrayStyle.Uint8ClampedArray, length){
			
			// Always uses a buffer as they are a byte[] anyway:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
		}
		
		public Uint8ClampedArray(double length):base(TypedArrayStyle.Uint8ClampedArray, (int)length){
			
			// Always uses a buffer as they are a byte[] anyway:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
		}
		
		public Uint8ClampedArray(TypedArray array):base(TypedArrayStyle.Uint8ClampedArray, array)
		{
			
			// Create a new buffer:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Get a quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
			// Now add the array:
			Add(array);
			
		}
		
		public Uint8ClampedArray(object iterableObj):base(TypedArrayStyle.Uint8ClampedArray, iterableObj)
		{
			
			// Create a new buffer:
			_Buffer=new ArrayBuffer(ByteLength);
			
			// Get a quick ref to the buffers buffer:
			buffer=_Buffer.buffer;
			
			// Now add it:
			Add(iterableObj);
			
		}
		
		public Uint8ClampedArray(ArrayBuffer buff):this(buff,0,0){}
		
		public Uint8ClampedArray(ArrayBuffer buff,int byteOffset,int length):base(TypedArrayStyle.Uint8ClampedArray, length==0?buff.ByteLength:length)
		{
			ByteOffset=byteOffset;
			_Buffer=buff;
			buffer=_Buffer.buffer;
		}
		
		/// <summary>Gets the value at the given index.</summary>
		protected override object Get(int index)
		{
			return buffer[index + ByteOffset];
		}
		
		/// <summary>
		/// Puts an unknown object into this array.
		/// Note that the value is always expected to be a value type.
		/// </summary>
		protected override void Set(int index,object rawValue)
		{
			
			// Clamp it:
			int value=(int)rawValue;
			
			if(value<0)
			{
				value=0;
			}else if(value>255)
			{
				value=255;
			}
			
			// Get it as a byte and put it in:
			buffer[index + ByteOffset]=(byte)value;
		}
		
		/// <summary>
		/// Gets or sets the given entry in the array.
		/// </summary>
		public int this[int index]{
			get{
				return buffer[index + ByteOffset];
			}
			set{
				
				if(value<0)
				{
					value=0;
				}else if(value>255)
				{
					value=255;
				}
				
				buffer[index + ByteOffset]=(byte)value;
			}
		}
		
		/// <summary>Creates a Uint8ClampedArray from the given iterable object.</summary>
		public static Uint8ClampedArray From(object value)
		{
			return new Uint8ClampedArray(value);
		}
		
	}
	
}