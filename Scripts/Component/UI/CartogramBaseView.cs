using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CartogramBaseView : BaseMonoBehaviour
{
    public List<CartogramDataBean> listCartogramData;

    public virtual void SetData(List<CartogramDataBean> listCartogramData)
    {
        this.listCartogramData = listCartogramData;
        InitCartogram();
    }

    public virtual void InitCartogram()
    {

    }
}