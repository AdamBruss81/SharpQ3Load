namespace obsvr
{
	public class Change
	{
		int m_nCode = -1;
		string m_sMessage = "";
		Subject m_pCallerSubject = null;
		Subject m_pOriginatingSubject = null;

		public Change(Subject pCallerSubject, Subject pOriginatingSubject, int nCode, string sMessage)
		{ 
			m_pCallerSubject = pCallerSubject; 
			m_pOriginatingSubject = pOriginatingSubject; 
			m_nCode = nCode;
			m_sMessage = sMessage;
		}

		public Subject GetCallerSubject
		{
			get { return m_pCallerSubject; }
		}

		public Subject GetOriginatingSubject
		{
			get { return m_pOriginatingSubject; }
		}

		public string GetMsg
		{
			get { return m_sMessage; }
		}

		public int GetCode 
		{
			get { return m_nCode; }
		}
	
		public void Set(Subject pCallerSubject, Subject pOriginatingSubject, int nCode, string sMessage)
		{ 
			m_pCallerSubject = pCallerSubject; 
			m_pOriginatingSubject = pOriginatingSubject; 
			m_nCode = nCode;
			m_sMessage = sMessage;
		}		
	};
}
