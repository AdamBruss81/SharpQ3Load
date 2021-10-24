using System;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace engine
{
	public class Zipper
	{
		protected FastZip zipper = new FastZip();

        public void ExtractToCustomTargetDir(string sZip, string sInternalPath, string sTargetDir)
        {
            if (File.Exists(sZip))
                zipper.ExtractZip(sZip, sTargetDir, sInternalPath);
        }

        public string ExtractMap(string sInternalPath)
		{
            if (string.IsNullOrEmpty(sInternalPath)) throw new Exception("Invalid path to extract");

            string sPath = Path.Combine(PATHS.GetTempDir, sInternalPath);

            if (!File.Exists(sPath))
                zipper.ExtractZip(PATHS.GetMapsZipFile, PATHS.GetTempDir, sInternalPath);

            System.Diagnostics.Debug.Assert(File.Exists(sPath));
            return sPath;
		}

		public string ExtractLightmap(string sInternalPath)
		{            
            if (string.IsNullOrEmpty(sInternalPath)) throw new Exception("Invalid path to extract");

            string sFullPathToExtractedFile = Path.Combine(PATHS.GetTempDir, sInternalPath);
            if (!File.Exists(sFullPathToExtractedFile))
            {
                GameGlobals.m_ZipExtractLMMutex.WaitOne();
                zipper.ExtractZip(PATHS.GetLightmapPath, PATHS.GetTempDir, sInternalPath);
                GameGlobals.m_ZipExtractLMMutex.ReleaseMutex();
            }

            return sFullPathToExtractedFile;
        }

        public string ExtractFromPakToDefaultTempDir(string sInternalPath, string sPAKFile = "")
        {
            if (string.IsNullOrEmpty(sInternalPath)) throw new Exception("Invalid path to extract");

            if (sPAKFile == "") sPAKFile = PATHS.GetPak0Path;

            string sFullPathToExtractedFile = Path.Combine(PATHS.GetTempDir, sInternalPath);
            GameGlobals.m_ZipExtractPak0Mutex.WaitOne();
            if (!File.Exists(sFullPathToExtractedFile))
            {
                sInternalPath = sInternalPath.Replace("\\", "/"); // the custom map city1 hit a regex exception on load. this fixed it and seems ok for other maps.
                zipper.ExtractZip(sPAKFile, PATHS.GetTempDir, sInternalPath);                
            }
            GameGlobals.m_ZipExtractPak0Mutex.ReleaseMutex();
            return sFullPathToExtractedFile;
		}

        public void ExtractAllShaderFiles(string sZipFile = "", string sTempDir = "")
        {
            if (sZipFile == "") sZipFile = PATHS.GetPak0Path;
            if (sTempDir == "") sTempDir = PATHS.GetTempDir;
            zipper.ExtractZip(sZipFile, sTempDir, FastZip.Overwrite.Never, null, "scripts/*.*", "", false);
        }

        public string ExtractShaderFile(string sShaderName)
        {
            if (string.IsNullOrEmpty(sShaderName)) throw new Exception("Invalid path to extract");

            string sFullPathToExtractedFile = Path.Combine(PATHS.GetTempDir, "scripts/" + sShaderName + ".shader");
            if (!File.Exists(sFullPathToExtractedFile))
            {
                zipper.ExtractZip(PATHS.GetPak0Path, PATHS.GetTempDir, "scripts/" + sShaderName + ".shader");
            }
            return sFullPathToExtractedFile;         
        }

        public void UpdateMap(string sMapPathOnDisk, string sInternalZipPath)
		{
			if (!File.Exists(PATHS.GetMapsZipFile)) 
				throw new Exception("Could not find map archive to update at " + PATHS.GetMapsZipFile);

			ZipFile file = new ZipFile(PATHS.GetMapsZipFile);

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
