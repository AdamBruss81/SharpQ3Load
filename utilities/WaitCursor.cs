using System;
using System.Windows.Forms;

namespace utilities
{
	public class WaitCursor : IDisposable
	{
		Cursor m_cursor;

		public WaitCursor() 
		{
			m_cursor = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;
		}
		
		public void Dispose()
		{	
			Cursor.Current = m_cursor;
		}
	}
}
