<div align="center">

<h1>

Antelcat.`{I18N}`

</h1>

给.NET应用程序提供响应式的多语言支持。

</div>

<p align="center">
    <img alt="dotnet-version" src="https://img.shields.io/badge/WPF-%3E%3D4.0-2C896A.svg"/>
<img alt="dotnet-version" src="https://img.shields.io/badge/Avalonia-%3E%3D11.0-AE42F8.svg"/>
    <img alt="csharp-version" src="https://img.shields.io/badge/C%23->=9.0-3BA93F.svg"/>
    <img alt="nuget" src="https://img.shields.io/badge/Nuget-v1.0.1-blue.svg"/>
</p>

---

🇬🇧 [English](./README.en.md)

## 🗔 受支持的平台

+ [WPF](https://github.com/dotnet/wpf)
+ [Avalonia](https://github.com/AvaloniaUI/Avalonia)

## 📖 示例

<div float="right">
    <img src="docs/demo.zh.png" width="45%"/>
    <img src="docs/demo.en.png" width="45%"/> 
</div>

---

### 静态使用

当你在项目中使用`.resx`文件作为语言文件时，你可以使用`Antelcat.I18N.Attributes.ResourceKeysOfAttribute`来自动生成资源键：

```csharp
using Antelcat.I18N.Attributes;

namespace MyProject

//Auto generated class should be partial
[ResourceKeysOf(typeof(My.Resource.Designer.Type))]
public partial class LangKeys;
```

+ 在 `Avalonia` 平台使用2.0.0以上版本时需要在 `App.axaml` 中调用

    ```csharp
    LangKeys.Your_Provider.Initialize();
    ```
    `WPF` 平台可以忽略这一步骤


然后在你的`.xaml`文件中使用`x:Static`来为你的控件提供资源键

如果你已经在你的`.resx`文件中有

```xml

<data name="Language" xml:space="preserve">
    <value>语言</value>
</data>
```

你可以像这样使用：

```xaml
<TextBolck Text="{x:Static myProject:LangKeys.Language}"/>
```

然后你可以使用这个键来绑定语言源

```xaml
<TextBlock Text="{I18N {x:Static myProject:LangKeys.Language}}"/>
```

当你想要改变语言时，只需要调用

```csharp
using System.Windows;

I18NExtension.Culture = new CultureInfo("language code");
```

你可以看到文本在语言之间变化。

---

### 动态使用

有时你的源文本并不是在你的应用程序中定义的，而是从其他来源（如网络）接收到的，你可以使用`I18N`直接绑定文本。

如果你收到了一个像这样的json：

```json
{
  "message": "This is a message"
}
```

并且你已经在`.resx`中将他翻译成了另一种语言

```xml

<data name="This is a message" xml:space="preserve">
    <value>这是一条消息</value>
</data>
```

你肯定会设计一个`ViewModel`并且将他设置到属性`Message`中，你可以像这样绑定：

```xaml
<!--他的DataContext就是你的ViewModel-->
<TextBlock Text="{I18N {Binding Message}}"/> 
```

每当`Message`属性被改变或者语言源被改变时，文本都会自动更新。

---

### 多个文本组合和格式化

有些情况下，你需要将多个文本组合起来，或者对文本进行格式化，你可以使用`I18N`和`LanguageBinding`来实现。

如果你已经有了如下翻译的`.resx`文件：

```xml

<data name="Current_is" xml:space="preserve">
    <value>当前的 {0} 是 {1}</value>
</data>
<data name="Language" xml:space="preserve">
    <value>语言</value>
</data>
<data name="Chinese" xml:space="preserve">
    <value>中文</value>
</data>
```

并且在`.xaml`中

```xaml
<TextBlock>
    <TextBlock.Text>
        <I18N Key="{x:Static myProject:LangKeys.Current_is}">
            <LanguageBinding Key="{x:Static myProject:LangKeys.Language}"/>
            <Binding Path="Language"/> <!--source text from view model-->
        </I18N>
    </TextBlock.Text>
</TextBlock>
```

此时 `I18N.Key` 是字符串的模板，其中的 `LanguageBinding` 和 `Binding` 会提供模板的参数，他们会被按顺序格式化成最终的文本。同时保持整体的响应性。