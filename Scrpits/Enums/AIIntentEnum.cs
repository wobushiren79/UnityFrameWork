using UnityEditor;
using UnityEngine;

public enum AIIntentEnum
{
    AnimalIdle,//闲置
    AnimalStroll,//闲逛
    AnimalRest,//休息
    AnimalEscape,//逃跑
    AnimalDead,//死亡

    MonsterIdle ,//闲置
    MonsterStroll,//闲逛
    MonsterChase,//追逐
    MonsterAttackMelee,//近战攻击
    MonsterAttackRemote,//远程攻击
    MonsterDead,//死亡
}