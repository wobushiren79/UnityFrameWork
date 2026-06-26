#ifndef STENCIL_TEST_INCLUDED
#define STENCIL_TEST_INCLUDED

// 模板测试宏定义
// 使用方法：
// 1. 在Properties中添加模板测试相关属性
// 2. 在Pass中使用APPLY_STENCIL_TEST宏应用模板测试

// 模板测试属性定义，在Properties块中使用
#define DECLARE_STENCIL_PROPERTIES \
        [HideInInspector] _EnableStencilTest ("Enable Stencil Test", Float) = 0 \
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8 \
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0 \
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0 \
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255 \
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255

// 模板测试配置宏
#define APPLY_STENCIL_TEST \
    Stencil \
    { \
        Ref [_Stencil] \
        Comp [_StencilComp] \
        Pass [_StencilOp] \
        ReadMask [_StencilReadMask] \
        WriteMask [_StencilWriteMask] \
    }

#endif // STENCIL_TEST_INCLUDED
