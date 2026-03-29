--[[

Aseprite 到 Spine 导出脚本
原作者: Jordan Bleu
https://github.com/jordanbleu/aseprite-to-spine

优化: 支持重名图层导出，按层级优先（上层覆盖下层）规则处理重复图片

]]

-----------------------------------------------[[ 函数定义 ]]-----------------------------------------------

--[[
递归获取所有图层（展平视图），包含图层组和普通图层。
parent: 精灵或父级图层组
arr: 追加结果的数组
]]
function getLayers(parent, arr)
    for i, layer in ipairs(parent.layers) do
        if (layer.isGroup) then
            arr[#arr + 1] = layer
            arr = getLayers(layer, arr)
        else
            arr[#arr + 1] = layer
        end
    end
    return arr
end

--[[
检查是否存在重名的可见图层，如果有则输出警告（不再阻止导出）。
layers: 展平后的图层列表
返回: 重名图层名称的集合（用于后续提示）
]]
function checkDuplicates(layers)
    local nameCount = {}
    local duplicateNames = {}

    for i, layer in ipairs(layers) do
        if (layer.isVisible and not layer.isGroup) then
            local name = layer.name
            nameCount[name] = (nameCount[name] or 0) + 1
        end
    end

    for name, count in pairs(nameCount) do
        if count > 1 then
            duplicateNames[name] = true
        end
    end

    return duplicateNames
end

--[[
获取每个图层的可见性状态数组。
layers: 展平后的图层列表
]]
function captureVisibilityStates(layers)
    local visibilities = {}
    for i, layer in ipairs(layers) do
        visibilities[i] = layer.isVisible
    end
    return visibilities
end

--[[
隐藏所有图层（图层组保持可见以便子图层可被访问）。
layers: 展平后的图层列表
]]
function hideAllLayers(layers)
    for i, layer in ipairs(layers) do
        if (layer.isGroup) then
            layer.isVisible = true
        else
            layer.isVisible = false
        end
    end
end

--[[
解析图层名称，提取前缀、数字和后缀。
如果名称中包含数字，返回 prefix, number, suffix；否则返回 nil。
示例: "A1AA" -> "A", 1, "AA"
      "A5AA" -> "A", 5, "AA"
      "AAA"  -> nil（无数字）
]]
function parseLayerName(name)
    local prefix, numStr, suffix = string.match(name, "^(.-)(%d+)(.*)$")
    if numStr then
        return prefix, tonumber(numStr), suffix
    end
    return nil
end

--[[
根据前缀、数字和后缀构建帧名称。
示例: "A", 3, "AA" -> "A3AA"
]]
function buildFrameName(prefix, num, suffix)
    return prefix .. tostring(num) .. suffix
end

--[[
检查图像是否完全透明（所有像素的 alpha 值均为 0）。
如果图像无可见内容则返回 true。
]]
function isImageEmpty(image)
    for pixel in image:pixels() do
        if app.pixelColor.rgbaA(pixel()) > 0 then
            return false
        end
    end
    return true
end

--[[
将每个图层导出为独立的 PNG 图片，忽略隐藏图层。
对于重名图层：在 Aseprite 中索引更大的图层层级更高（显示在上方），
后处理的同名图层会覆盖先处理的，从而实现上层优先。

多帧图层规则:
  - 图层名含数字（如 "A1AA"）: 按帧数递增导出 A1AA, A2AA, A3AA, ...
  - 图层名无数字（如 "AAA"）: 仅导出第一帧。

layers: 展平后的图层列表
sprite: 当前活动精灵
visibilityStates: 各图层导出前的可见性状态
]]
function captureLayers(layers, sprite, visibilityStates)
    hideAllLayers(layers)

    local outputDir = app.fs.filePath(sprite.filename)
    local spriteFileName = app.fs.fileTitle(sprite.filename)

    local jsonFileName = outputDir .. app.fs.pathSeparator .. spriteFileName .. ".json"
    json = io.open(jsonFileName, "w")

    json:write('{')

    -- 骨骼信息
    json:write([[ "skeleton": { "images": "images/" }, ]])

    -- 骨骼节点
    json:write([[ "bones": [ { "name": "root" }	], ]])

    -- 构建 slots 和 skins 的 JSON 数据
    -- 使用有序表记录帧名称，重名时后出现的（层级更高的）覆盖前面的
    local frameDataMap = {}    -- frameName -> { slotJson, skinJson }
    local frameOrder = {}      -- 按首次出现顺序记录帧名称

    local separator = app.fs.pathSeparator

    for i, layer in ipairs(layers) do
        -- 忽略图层组和不可见图层
        if (not layer.isGroup and visibilityStates[i] == true) then
            layer.isVisible = true

            local prefix, startNum, suffix = parseLayerName(layer.name)
            local frameCount = 1

            if prefix then
                -- 图层名含数字：按精灵帧数导出所有帧
                frameCount = #sprite.frames
            end

            for f = 1, frameCount do
                -- 获取指定帧号的 cel
                local cel = layer:cel(f)
                -- 跳过无内容的帧（cel 为空或图像完全透明）
                if cel and not isImageEmpty(cel.image) then
                    local frameName
                    if prefix then
                        frameName = buildFrameName(prefix, startNum + (f - 1), suffix)
                    else
                        frameName = layer.name
                    end

                    -- 从 cel 图像直接创建新精灵，避免复制多帧精灵时的帧删除问题
                    local cropped = Sprite(cel.bounds.width, cel.bounds.height, sprite.colorMode)
                    cropped.cels[1].image = cel.image:clone()
                    cropped:saveCopyAs(outputDir .. separator .. "images" .. separator .. frameName .. ".png")
                    cropped:close()
                    app.activeSprite = sprite

                    local slotJson = string.format([[ { "name": "%s", "bone": "%s", "attachment": "%s" } ]], frameName, "root", frameName)
                    local skinJson = string.format([[ "%s": { "%s": { "x": %.2f, "y": %.2f, "width": 1, "height": 1 } } ]], frameName, frameName, cel.bounds.width/2 + cel.position.x - sprite.bounds.width/2, sprite.bounds.height - cel.position.y - cel.bounds.height/2)

                    -- 如果该帧名已存在，先从顺序表中移除旧位置
                    if frameDataMap[frameName] then
                        for k = #frameOrder, 1, -1 do
                            if frameOrder[k] == frameName then
                                table.remove(frameOrder, k)
                                break
                            end
                        end
                    end
                    -- 追加到顺序表末尾，确保绘制顺序与图层层级一致
                    frameOrder[#frameOrder + 1] = frameName
                    -- 直接覆盖：后处理的图层层级更高，优先显示
                    frameDataMap[frameName] = { slot = slotJson, skin = skinJson }
                end
            end

            layer.isVisible = false
        end
    end

    -- 按首次出现顺序构建最终的 JSON 数组
    local slotsJson = {}
    local skinsJson = {}
    for idx, frameName in ipairs(frameOrder) do
        local data = frameDataMap[frameName]
        slotsJson[idx] = data.slot
        skinsJson[idx] = data.skin
    end

    -- 插槽
    json:write('"slots": [')
    json:write(table.concat(slotsJson, ","))
    json:write("],")

    -- 皮肤
    json:write('"skins": {')
    json:write('"default": {')
    json:write(table.concat(skinsJson, ","))
    json:write('}')
    json:write('}')

    -- 关闭 JSON 文件
    json:write("}")

    json:close()

    app.alert("导出完成！使用文件 '" .. jsonFileName .. "' 导入到 Spine。")
end

--[[
恢复图层到导出前的可见性状态。
layers: 展平后的图层列表
visibilityStates: 各图层导出前的可见性状态
]]
function restoreVisibilities(layers, visibilityStates)
    for i, layer in ipairs(layers) do
        layer.isVisible = visibilityStates[i]
    end
end

-----------------------------------------------[[ 主执行流程 ]]-----------------------------------------------
local activeSprite = app.activeSprite

if (activeSprite == nil) then
    -- 用户未选中任何精灵
    app.alert("请先点击要导出的精灵")
    return
elseif (activeSprite.filename == "") then
    -- 精灵已创建但尚未保存
    app.alert("请先保存当前精灵再运行此脚本")
    return
end

local flattenedLayers = getLayers(activeSprite, {})

-- 检查重名图层并输出警告（不阻止导出）
local duplicateNames = checkDuplicates(flattenedLayers)
local dupList = {}
for name, _ in pairs(duplicateNames) do
    dupList[#dupList + 1] = name
end
if #dupList > 0 then
    app.alert("发现重名图层: " .. table.concat(dupList, ", ") .. "\n将按层级优先规则导出（上层覆盖下层）。")
end

-- 记录每个图层当前的可见性状态
local visibilities = captureVisibilityStates(flattenedLayers)

-- 将每个图层导出为独立的 PNG 文件，并生成 Spine 导入用的 JSON 文件
captureLayers(flattenedLayers, activeSprite, visibilities)

-- 恢复图层的可见性到导出前的状态
restoreVisibilities(flattenedLayers, visibilities)