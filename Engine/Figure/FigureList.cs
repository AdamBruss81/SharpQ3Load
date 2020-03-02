//*===================================================================================
//* ----||||Simulator||||----
//*
//* By Adam Bruss and Scott Nykl
//*
//* Scott participated in Fall of 2005. Adam has participated from fall 2005 
//* until the present.
//*
//* Loads in quake 3 m_maps. Three modes of interaction are Player, Ghost and Spectator.
//*===================================================================================

using System;
using System.Collections.Generic;
using utilities;

namespace engine
{
	/// <summary>
	/// List of figures
	/// </summary>
    public class FigureList
    {
        private List<Figure> m_lFigures = new List<Figure>();

        public void Add( Figure f ) { m_lFigures.Add( f ); }

        public bool Remove( int figureID )
        {
            for( int i = 0; i < m_lFigures.Count; i++ )
            {
                if ( m_lFigures[i].GetFigureID == figureID )
                {
                   m_lFigures.RemoveAt( i );
                   return true;
                }
            }
            return false;
        }

        public void Clear(bool bDelete)
        {
			if(bDelete) DeleteAll();

            m_lFigures.Clear();
        }

        public Figure getFigure(int figureID)
        {
            for ( int i = 0; i < m_lFigures.Count; i++ )
            {
                if (m_lFigures[i].GetFigureID == figureID)
                    return m_lFigures[i];
            }
            return null;
        }

        public int Count( )
        { 
            return m_lFigures.Count; 
        }

        public Figure this[int index]
        {
            get 
            {
                if( m_lFigures.Count > 0 )
                   return m_lFigures[index];
                return null;
            }  
        }

        public void ShowAllFigures(Engine.EGraphicsMode mode, ref int nCount, List<Plane> lFrustrum, MovableCamera cam)
        {
            for( int i = 0; i < m_lFigures.Count; i++ )
            {
				m_lFigures[i].Show(mode, ref nCount, lFrustrum, cam);
            }
        }

		public void DeleteAll()
		{
			foreach(Figure f in m_lFigures)
			{
				f.Delete();
			}
		}
    }
}

