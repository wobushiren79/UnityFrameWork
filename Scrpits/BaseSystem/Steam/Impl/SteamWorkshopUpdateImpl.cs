using UnityEngine;
using UnityEditor;
using Steamworks;
using System.Collections;

public class SteamWorkshopUpdateImpl : ISteamWorkshopUpdate
{
    private AppId_t mAppId;
    private readonly BaseMonoBehaviour mContent;

    //上传回调
    private ISteamWorkshopUpdateCallBack mUpdateCallBack;
    //上传数据
    private SteamWorkshopUpdateBean mUpdateData;
    //上传数据
    private UGCUpdateHandle_t mUpdateHandle;
    //是否上传完毕
    private bool mIsComplete = true;

    public enum SteamWorkshopUpdateFailEnum
    {
        REQUEST_FAIL = 0,//网络请求失败
        NO_TITLE = 1, //没有标题
        NO_DESCRIPTION = 2, //没有介绍
        NO_TAGS=3,//没有标签
        NO_CONTENT=4,//没有内容路径
        NO_PREVIEW = 5,//没有浏览图路径
        PREVIEW_BIG = 6,//浏览图太大 必须小于1M
        NO_UPDATEDATA = 100,//没有上传数据
    }

    public SteamWorkshopUpdateImpl(BaseMonoBehaviour mContent)
    {
        //初始化appid
        this.mAppId = new AppId_t(uint.Parse(ProjectConfigInfo.STEAM_APP_ID));
        this.mContent = mContent;
    }

    public void CreateWorkshopItem(SteamWorkshopUpdateBean updateBean, ISteamWorkshopUpdateCallBack callBack)
    {
        this.mUpdateData = updateBean;
        this.mUpdateCallBack = callBack;

        //检测是否有上传数据
        if (updateBean == null)
        {
            callBack.UpdateFail(SteamWorkshopUpdateFailEnum.NO_UPDATEDATA);
            return;
        }
        //检测是否有标题
        if (updateBean.title.IsNull())
        {
            callBack.UpdateFail(SteamWorkshopUpdateFailEnum.NO_TITLE);
            return;
        }
        //检测是否有介绍
        if (updateBean.description.IsNull())
        {
            callBack.UpdateFail(SteamWorkshopUpdateFailEnum.NO_DESCRIPTION);
            return;
        }
        //检测是否有标签
        if (updateBean.tags.IsNull())
        {
            callBack.UpdateFail(SteamWorkshopUpdateFailEnum.NO_TAGS);
            return;
        }
        //检测是否有文件路径
        if (updateBean.content.IsNull())
        {
            callBack.UpdateFail(SteamWorkshopUpdateFailEnum.NO_CONTENT);
            return;
        }
        //检测是否有浏览图路径
        if (updateBean.preview.IsNull())
        {
            callBack.UpdateFail(SteamWorkshopUpdateFailEnum.NO_PREVIEW);
            return;
        }
        CallResult<CreateItemResult_t> callResult = CallResult<CreateItemResult_t>.Create(OnCreateItemCallBack);
        SteamAPICall_t apiCall = SteamUGC.CreateItem(mAppId, EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        callResult.Set(apiCall);
    }


    /// <summary>
    /// CreateItem回调
    /// </summary>
    /// <param name="itemResult"></param>
    /// <param name="bIOFailure"></param>
    private void OnCreateItemCallBack(CreateItemResult_t itemResult, bool bIOFailure)
    {
        if(bIOFailure|| itemResult.m_eResult != EResult.k_EResultOK)
        {
            SteamUGC.DeleteItem(itemResult.m_nPublishedFileId);
            if (this.mUpdateCallBack != null)
                this.mUpdateCallBack.UpdateFail(SteamWorkshopUpdateFailEnum.REQUEST_FAIL);
            return;
        }

        if (itemResult.m_eResult == EResult.k_EResultOK)
        {
            SetCreateItemInfo(itemResult.m_nPublishedFileId);
        }
    }

    /// <summary>
    /// 提交item回调
    /// </summary>
    /// <param name="pCallback"></param>
    /// <param name="bIOFailure"></param>
    private void OnSubmitItemCallBack(SubmitItemUpdateResult_t itemResult, bool bIOFailure)
    {
        mIsComplete = true;
        if (bIOFailure || itemResult.m_eResult != EResult.k_EResultOK)
        {
            SteamUGC.DeleteItem(itemResult.m_nPublishedFileId);
            mContent.StopCoroutine(ProgressCoroutine(mUpdateHandle, itemResult.m_nPublishedFileId));
            if (this.mUpdateCallBack != null)
                if(itemResult.m_eResult== EResult.k_EResultLimitExceeded)
                {
                    this.mUpdateCallBack.UpdateFail(SteamWorkshopUpdateFailEnum.PREVIEW_BIG);
                }
                else
                {
                    this.mUpdateCallBack.UpdateFail(SteamWorkshopUpdateFailEnum.REQUEST_FAIL);
                }
            return;
        }
        if (this.mUpdateCallBack != null)
            this.mUpdateCallBack.UpdateSuccess();
    }

    /// <summary>
    /// 设置item的相关数据
    /// </summary>
    /// <param name="fileId"></param>
    private void SetCreateItemInfo(PublishedFileId_t fileId)
    {
        //获取设置参数基类
        mUpdateHandle = SteamUGC.StartItemUpdate(mAppId, fileId);
        //设置标题
        SteamUGC.SetItemTitle(mUpdateHandle, this.mUpdateData.title);
        //设置介绍
        SteamUGC.SetItemDescription(mUpdateHandle, this.mUpdateData.description);
        //设置语言
        SteamUGC.SetItemUpdateLanguage(mUpdateHandle, this.mUpdateData.updateLanguage);
        //设置原数据
        SteamUGC.SetItemMetadata(mUpdateHandle, this.mUpdateData.metadata);
        //设置公开
        SteamUGC.SetItemVisibility(mUpdateHandle, this.mUpdateData.visibility);
        //设置标签
        SteamUGC.SetItemTags(mUpdateHandle, this.mUpdateData.tags);
        //设置键值对
        //SteamUGC.AddItemKeyValueTag();
        //移除键值对
        //SteamUGC.RemoveItemKeyValueTags();
        //设置文件地址
        SteamUGC.SetItemContent(mUpdateHandle, this.mUpdateData.content);
        //设置浏览图片
        SteamUGC.SetItemPreview(mUpdateHandle, this.mUpdateData.preview);

        CallResult<SubmitItemUpdateResult_t> callResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemCallBack);
        SteamAPICall_t apiCallBack = SteamUGC.SubmitItemUpdate(mUpdateHandle, null);
        callResult.Set(apiCallBack);
        mContent.StartCoroutine(ProgressCoroutine(mUpdateHandle,  fileId));
    }

    /// <summary>
    /// 开始上传进度协程跟进
    /// </summary>
    /// <param name="updateHandle"></param>
    /// <returns></returns>
    private IEnumerator ProgressCoroutine(UGCUpdateHandle_t updateHandle, PublishedFileId_t fileId)
    {
        mIsComplete = false;
        while (!mIsComplete)
        {
            ulong progressBytes;
            ulong totalBytes;
            EItemUpdateStatus state = SteamUGC.GetItemUpdateProgress(updateHandle, out progressBytes, out totalBytes);
            if (this.mUpdateCallBack != null)
                if (state == EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges)
                {
                    mIsComplete = true;
                    this.mUpdateCallBack.UpdateSuccess();
                }
                else if (state == EItemUpdateStatus.k_EItemUpdateStatusInvalid)
                {
                    mIsComplete = true;
                    this.mUpdateCallBack.UpdateFail(SteamWorkshopUpdateFailEnum.REQUEST_FAIL);
                    SteamUGC.DeleteItem(fileId);
                }
                else
                {
                    this.mUpdateCallBack.UpdateProgress(state, progressBytes, totalBytes);
                }
            yield return new WaitForSeconds(0.1f);
        }
    }
}