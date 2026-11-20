--[[

Aseprite 到 Spine 导出脚本
原作者: Jordan Bleu
https://github.com/jordanbleu/aseprite-to-spine

可用 Spine-Export-Marking 标记需要跳过的 Cel。同名附件采用最上层可导出的 Cel。

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

local function isExcluded(cel)
    for line in cel.data:gmatch("[^\r\n]+") do
        if line == "[spine:skip]" then return true end
    end
    return false
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
]]
function captureLayers(layers, sprite)
    local outputDir = app.fs.filePath(sprite.filename)
    local spriteFileName = app.fs.fileTitle(sprite.filename)
    local imagesDir = app.fs.joinPath(outputDir, "images")
    local jsonFileName = app.fs.joinPath(outputDir, spriteFileName .. ".json")
    local candidates = {}
    local topCel = {}
    local markedNames = {}
    local markedCount = 0

    for _, layer in ipairs(layers) do
        if not layer.isGroup then
            local prefix, startNum, suffix = parseLayerName(layer.name)
            local frameCount = prefix and #sprite.frames or 1
            for frame = 1, frameCount do
                local cel = layer:cel(frame)
                if cel then
                    local name = prefix and buildFrameName(prefix, startNum + frame - 1, suffix) or layer.name
                    if isExcluded(cel) then
                        markedNames[name] = true
                        markedCount = markedCount + 1
                    elseif layer.isVisible and not isImageEmpty(cel.image) then
                        candidates[#candidates + 1] = { name = name, cel = cel }
                        topCel[name] = cel
                    end
                end
            end
        end
    end

    if not app.fs.isDirectory(imagesDir) then
        assert(app.fs.makeDirectory(imagesDir), "无法创建分件目录 " .. imagesDir)
    end
    -- 只写最终采用的 Cel，PNG 和 JSON 按同一套排除与重名规则生成。
    local slotsJson = {}
    local skinsJson = {}
    for _, candidate in ipairs(candidates) do
        local name, cel = candidate.name, candidate.cel
        if topCel[name] == cel then
            local cropped = Sprite(cel.bounds.width, cel.bounds.height, sprite.colorMode)
            cropped.cels[1].image = cel.image:clone()
            cropped:saveCopyAs(app.fs.joinPath(imagesDir, name .. ".png"))
            cropped:close()
            app.activeSprite = sprite

            -- Aseprite 只编码表，取单项数组括号内的 JSON 字符串以保留名称转义。
            local key = json.encode({ name }):sub(2, -2)
            local x = cel.bounds.width / 2 + cel.position.x - sprite.bounds.width / 2
            local y = sprite.bounds.height - cel.position.y - cel.bounds.height / 2
            slotsJson[#slotsJson + 1] = string.format([[ { "name": %s, "bone": "root", "attachment": %s } ]], key, key)
            skinsJson[#skinsJson + 1] = string.format([[ %s: { %s: { "x": %.2f, "y": %.2f, "width": 1, "height": 1 } } ]], key, key, x, y)
        end
    end

    local file = assert(io.open(jsonFileName, "w"))
    assert(file:write('{', [[ "skeleton": { "images": "images/" }, ]],
        [[ "bones": [ { "name": "root" }	], ]], '"slots": [', table.concat(slotsJson, ","),
        '],"skins": {"default": {', table.concat(skinsJson, ","), '}}}'))
    assert(file:close())

    -- 增量发布根据这份清单移除旧附件，或以同名候选替换旧图与坐标。
    if not app.isUIAvailable then
        for name in pairs(markedNames) do
            if topCel[name] then
                print("SPINE_REPLACE " .. name)
            else
                print("SPINE_SKIP " .. name)
            end
        end
    end
    app.alert("导出完成！使用文件 '" .. jsonFileName .. "' 导入到 Spine。\n已跳过 " .. markedCount .. " 个标记的 Cel。")
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

-- 将每个图层导出为独立的 PNG 文件，并生成 Spine 导入用的 JSON 文件
captureLayers(flattenedLayers, activeSprite)
