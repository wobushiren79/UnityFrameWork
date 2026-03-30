using UnityEngine;
using System;
using System.IO;
using System.Text;

public class FileUtil
{

    /// <summary>
    /// 创建文本文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="strData"></param>
    public static void CreateTextFile(string filePath, string fileName, string strData)
    {
        CreateTextFile($"{filePath}/{fileName}", strData);
    }

    public static void CreateTextFile(string filePathName, string strData)
    {
        StreamWriter writer = null;
        try
        {
            DeleteFile(filePathName);
            writer = new StreamWriter(filePathName, false, Encoding.UTF8);
            writer.Write(strData);
        }
        catch (Exception e)
        {
            string strError = "创建文件失败-" + e.Message;
            LogUtil.LogWarning(strError);
        }
        finally
        {
            if (writer != null)
                writer.Close();
        }
    }


    /// <summary>
    /// 加载文本
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string LoadTextFile(string filePath)
    {
        StreamReader reader = null;
        try
        {
            reader = File.OpenText(filePath);
            String strData = reader.ReadToEnd();
            return strData;
        }
        catch (Exception e)
        {
            string strError = "读取文件失败-" + e.Message;
            LogUtil.LogWarning(strError);
            return null;
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }


    /// <summary>
    /// 打开文件选择弹窗
    /// </summary>
    /// <param name="filter">需要打开的文件类型</param>
    /// <returns></returns>
    //public static string OpenFileDialog(string filter)
    //{
    //    OpenFileDialog ofd = new OpenFileDialog();   //new一个方法
    //    ofd.InitialDirectory = "file://" + UnityEngine.Application.dataPath;  //定义打开的默认文件夹位置//定义打开的默认文件夹位置
    //    ofd.Filter = filter;
    //    if (ofd.ShowDialog() == DialogResult.OK)
    //    {
    //        return ofd.FileName;
    //    }
    //    else
    //    {
    //        return null;
    //    }
    //}

    /// <summary>
    /// 打开文件选择弹窗 for图片类型
    /// </summary>
    //public static string OpenFileDialogForPic()
    //{
    //    string filter = "(*.jpg,*.png,*.jpeg,*.bmp)|*.jpg;*.png;*.jpeg;*.bmp";
    //    return OpenFileDialog(filter);
    //}


    /// <summary>
    /// 复制文件到指定路径
    /// </summary>
    /// <param name="sourceFilePatch">源文件路径</param>
    /// <param name="saveFilePath">存储路径</param>
    /// <param name="isReplace">若存储路径有相同文件是否替换</param>
    public static bool CopyFile(string sourceFilePatch, string saveFilePath, bool isReplace)
    {
        if (File.Exists(sourceFilePatch))//必须判断要复制的文件是否存在
        {
            File.Copy(sourceFilePatch, saveFilePath, isReplace);     
            return true;
        }
        return false;
    }

    /// <summary>
    /// 创建文件夹
    /// </summary>
    /// <param name="directoryPath"></param>
    public static void CreateDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// 删除文件夹
    /// </summary>
    /// <param name="directoryPath"></param>
    public static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            DeleteAllFile(directoryPath);
            Directory.Delete(directoryPath);
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="filePath"></param>
    public static void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))//必须判断要删除的文件是否存在
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// 删除指定文件目录下的所有文件
    /// </summary>
    /// <param name="fullPath">文件路径</param>
    public static bool DeleteAllFile(string fullPath)
    {
        //获取指定路径下面的所有资源文件  然后进行删除
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo direction = new DirectoryInfo(fullPath);
            //删除所有文件
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Name.EndsWith(".meta"))
                {
                    continue;
                }
                string filePath = fullPath + "/" + files[i].Name;
                DeleteFile(filePath);
            }
            //删除所有文件夹
            DirectoryInfo[] directoryInfos = direction.GetDirectories();
            for (int i = 0; i < directoryInfos.Length; i++)
            {
                DeleteDirectory(directoryInfos[i].FullName);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 保存图片到本地
    /// </summary>
    /// <param name="filePathName"></param>
    /// <param name="tex"></param>
    public static void ImageSaveLocal(string filePathName, Texture tex)
    {
        Texture2D saveImageTex = tex as Texture2D;
        FileStream newFs = new FileStream(filePathName, FileMode.Create, FileAccess.Write);
        byte[] bytes = saveImageTex.EncodeToJPG();
        newFs.Write(bytes, 0, bytes.Length);
        newFs.Close();
        newFs.Dispose();
    }

    /// <summary>
    /// 获取指定路径下面的所有资源文件  
    /// </summary>
    /// <param name="fullPath"> "Assets/Models/21/21000001/"; </param>
    /// <returns></returns>
    public static FileInfo[] GetFilesByPath(string fullPath, SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo direction = new DirectoryInfo(fullPath);
            FileInfo[] files = direction.GetFiles("*", searchOption);
            return files;
        }
        return null;
    }

    /// <summary>
    /// 获取指定路径下的所有文件夹
    /// </summary>
    /// <param name="fullPath">"Assets/Models/21/21000001/"; </param>
    /// <returns></returns>
    public static DirectoryInfo[] GetDirectoriesByPath(string fullPath, SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo directory = new DirectoryInfo(fullPath);
            DirectoryInfo[] directories = directory.GetDirectories("*", searchOption);
            return directories;
        }
        return null;
    }
}