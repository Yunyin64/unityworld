# 场景 B：Data 嵌套在另一个 Data 中

当 Data 是另一个 Data 的子数据块（如 `NpcAppearanceData` 挂在 `NpcBioData` 中）。

文件整体结构与场景 A 相同（Data 类本体 + partial class），但 **partial class 中的访问器需要通过父 Data 间接访问**：

```csharp
    public partial class {Entity}
    {
        /// <summary>{子Data说明}</summary>
        public {Name}Data {ShortName} => {ParentDataAccessor}.{ChildFieldName};

        /// <summary>{字段便捷访问}</summary>
        public {ReturnType} {GetterName}() => {ShortName}.{Field};
    }
```

## 示例

`NpcAppearanceData` 嵌套在 `NpcBioData` 中：

```csharp
    public partial class Npc
    {
        // AppearanceData 属性已在 NpcBioData.cs 的 partial class 中定义：
        //   public NpcAppearanceData AppearanceData => BioData.AppearanceData;
        // 所以这里只需要暴露更深层的便捷访问器
        public float GetHeight() => AppearanceData.Height;
    }
```

## 要点

- 嵌套场景下，父级 Data 的 partial class 中已经定义了子 Data 的属性 getter
- 子 Data 自己的文件中只需要暴露更深一层的字段访问器
- 如果子 Data 又有自己的子 Data，同理递进