namespace obsvr.Test
{
	public class Watcher : Observer
	{
		public static int g_nNumUpdatedYelled = 0;
		public static int g_nNumUpdatedWhispered = 0;

		public override void Update(Change pChange)
		{
			if (pChange.GetCode == (int)Signaler.Signals.WHISPERED)
				g_nNumUpdatedWhispered++;
			else if (pChange.GetCode == (int)Signaler.Signals.YELLED)
				g_nNumUpdatedYelled++;
		}
	}
}
