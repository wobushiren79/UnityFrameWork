using UnityEngine;
using UnityEditor;
using Steamworks;
using System.Collections.Generic;

public class SteamWorkshopQueryImpl : ISteamWorkshopQuery
{
    private AppId_t mAppId;
    private readonly BaseMonoBehaviour mContent;

    //查询本地创意工坊物品回调
    private ISteamWorkshopQueryInstallInfoCallBack mQueryInstallInfoCallBack;

    public enum SteamWorkshopQueryFailEnum
    {
        REQUEST_FAIL = 0,//网络请求失败
    }

    public SteamWorkshopQueryImpl(BaseMonoBehaviour content)
    {
        //初始化appid
        this.mAppId = new AppId_t(uint.Parse(ProjectConfigInfo.STEAM_APP_ID));
        this.mContent = content;
    }

    public void QueryInstallInfo(uint pageNumber, ISteamWorkshopQueryInstallInfoCallBack callBack)
    {
        this.mQueryInstallInfoCallBack = callBack;

        UGCQueryHandle_t handle = SteamUGC.CreateQueryUserUGCRequest(SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_All, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderAsc, mAppId, mAppId, pageNumber);
        SteamUGC.SetReturnMetadata(handle,true);
        CallResult<SteamUGCQueryCompleted_t> callResult = CallResult<SteamUGCQueryCompleted_t>.Create(QueryUserUGCCallBack);
        SteamAPICall_t apiCall = SteamUGC.SendQueryUGCRequest(handle);
        callResult.Set(apiCall);
    }

    /// <summary>
    /// 查询与自己关联的创意工坊物品
    /// </summary>
    /// <param name="itemResult"></param>
    /// <param name="bIOFailure"></param>
    private void QueryUserUGCCallBack(SteamUGCQueryCompleted_t itemResult, bool bIOFailure)
    {
        if (bIOFailure || itemResult.m_eResult != EResult.k_EResultOK)
        {
            if (mQueryInstallInfoCallBack != null)
                mQueryInstallInfoCallBack.GetInstallInfoFail(SteamWorkshopQueryFailEnum.REQUEST_FAIL);
            return;
        }

        List<SteamWorkshopQueryInstallInfoBean> listInstallInfo = new List<SteamWorkshopQueryInstallInfoBean>();
        for (uint i = 0; i < itemResult.m_unNumResultsReturned; i++)
        {
            SteamUGCDetails_t detailsInfo;
            SteamUGC.GetQueryUGCResult(itemResult.m_handle, i, out detailsInfo);
            ulong punSizeOnDisk;
            string pchFolder;
            uint punTimeStamp;
            SteamUGC.GetItemInstallInfo(detailsInfo.m_nPublishedFileId, out punSizeOnDisk, out pchFolder, (uint)detailsInfo.m_nFileSize, out punTimeStamp);
            string metaData;
            SteamUGC.GetQueryUGCMetadata(itemResult.m_handle ,i , out metaData, 1000);
            //if (punSizeOnDisk == 0 || punTimeStamp == 0 || CheckUtil.StringIsNull(pchFolder))
            //{
            //    continue;
            //}
            //添加缩略图地址
            string previewUrl;
            SteamUGC.GetQueryUGCPreviewURL(itemResult.m_handle, i, out previewUrl, (uint)detailsInfo.m_nPreviewFileSize);

            SteamWorkshopQueryInstallInfoBean installInfoBean = new SteamWorkshopQueryInstallInfoBean
            {
                punSizeOnDisk = punSizeOnDisk,
                pchFolder = pchFolder,
                punTimeStamp = punTimeStamp,
                detailsInfo = detailsInfo,
                previewUrl = previewUrl,
                metaData = metaData
            };
        
            listInstallInfo.Add(installInfoBean);
        }
        if (mQueryInstallInfoCallBack != null)
            mQueryInstallInfoCallBack.GetInstallInfoSuccess(listInstallInfo);
    }
}