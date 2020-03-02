using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using System.IO;

namespace engine
{
	public class Zipper
	{
		protected FastZip zipper = new FastZip();

		public string ExtractMap(string sInternalPath)
		{
            string sPath = Path.Combine(PATHS.GetTempDir, sInternalPath);

            if (!File.Exists(sPath))
                zipper.ExtractZip(PATHS.GetProgramDataMapsZipFile, PATHS.GetTempDir, sInternalPath);

            System.Diagnostics.Debug.Assert(File.Exists(sPath));
            return sPath;
		}

		public string ExtractSoundTextureOther(string sInternalPath)
		{
			zipper.ExtractZip(PATHS.GetSoundsTexturesOtherZipFile, PATHS.GetTempDir, sInternalPath);
			string sFullPathToExtractedFile = Path.Combine(PATHS.GetTempDir, sInternalPath);
			return sFullPathToExtractedFile;
		}

		public void UpdateMap(string sMapPathOnDisk, string sInternalZipPath)
		{
			if (!File.Exists(PATHS.GetProgramDataMapsZipFile)) 
				throw new Exception("Could not find map archive to update at " + PATHS.GetProgramDataMapsZipFile);

			ZipFile file = new ZipFile(PATHS.GetProgramDataMapsZipFile);

			file.BeginUpdate();
			file.Delete(sInternalZipPath);
			file.CommitUpdate();
			file.BeginUpdate();
			file.Add(sMapPathOnDisk, sInternalZipPath);
			file.CommitUpdate();

			file.Close();
		}
	}
}
