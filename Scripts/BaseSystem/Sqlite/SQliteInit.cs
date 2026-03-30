using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Networking;
using System;

public class SQLiteInit : BaseMonoBehaviour
{
    private void Awake()
    {
		InitSQlite(ProjectConfigInfo.DATA_BASE_INFO_NAME);
	}

	private string pathDB;
	/// <summary>
	/// Initializes a new instance of the <see cref="SqliteDatabase"/> class.
	/// </summary>
	/// <param name='dbName'> 
	/// Data Base name. (the file needs exist in the streamingAssets folder)
	/// </param>
	public void InitSQlite(string dbName)
	{
		pathDB = System.IO.Path.Combine(Application.persistentDataPath, dbName);
		//original path
		string sourcePath = System.IO.Path.Combine(Application.streamingAssetsPath, "SQLiteDataBase/" + dbName);
		//if DB does not exist in persistent data folder (folder "Documents" on iOS) or source DB is newer then copy it
		//if (!System.IO.File.Exists(pathDB) || (System.IO.File.GetLastWriteTimeUtc(sourcePath) > System.IO.File.GetLastWriteTimeUtc(pathDB)))
		//{
			if (sourcePath.Contains("://"))
			{
				// Android	
#pragma warning disable CS0618 // 类型或成员已过时
				WWW www = new WWW(sourcePath);
#pragma warning restore CS0618 // 类型或成员已过时
				// Wait for download to complete - not pretty at all but easy hack for now 
				// and it would not take long since the data is on the local device.
				while (!www.isDone) {; }
				if (String.IsNullOrEmpty(www.error))
				{
					System.IO.File.WriteAllBytes(pathDB, www.bytes);
				}
				else
				{
					//CanExQuery = false;
				}
			}
			else
			{
				// Mac, Windows, Iphone
				//validate the existens of the DB in the original folder (folder "streamingAssets")
				if (System.IO.File.Exists(sourcePath))
				{
					//copy file - alle systems except Android
					System.IO.File.Copy(sourcePath, pathDB, true);
				}
				else
				{
					//CanExQuery = false;
					Debug.Log("ERROR: the file DB named " + dbName + " doesn't exist in the StreamingAssets Folder, please copy it there.");
				}
			}
		//}
	}

	public void InitSQliteSef()
    {
		if (Application.platform == RuntimePlatform.Android)
		{
			//将第三方数据库拷贝至Android可找到的地方
			string appDBPath = Application.persistentDataPath + "/" + ProjectConfigInfo.DATA_BASE_INFO_NAME;
			//如果已知路径没有地方放数据库，那么我们从Unity中拷贝
			if (File.Exists(appDBPath))
			{
				File.Delete(appDBPath);
			}
			//用www先从Unity中下载到数据库
#pragma warning disable CS0618 // 类型或成员已过时
			WWW loadDB = new WWW("jar:file://" + Application.dataPath + "!/assets/SQLiteDataBase/" + ProjectConfigInfo.DATA_BASE_INFO_NAME);
#pragma warning restore CS0618 // 类型或成员已过时
			while (!loadDB.isDone) { }
			//拷贝至规定的地方
			File.WriteAllBytes(appDBPath, loadDB.bytes);
		}
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			string appDBPath = Application.persistentDataPath + "/" + ProjectConfigInfo.DATA_BASE_INFO_NAME;
			if (File.Exists(appDBPath))
			{
				File.Delete(appDBPath);
			}
#pragma warning disable CS0618 // 类型或成员已过时
			WWW loadDB = new WWW("file://" + Application.streamingAssetsPath + "/SQLiteDataBase/" + ProjectConfigInfo.DATA_BASE_INFO_NAME);
#pragma warning restore CS0618 // 类型或成员已过时
			while (!loadDB.isDone) { }
			File.WriteAllBytes("file://" + appDBPath, loadDB.bytes);
		}
	}
}