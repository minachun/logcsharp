# logcsharp

## 概要
アプリケーション開発で動作追跡や原因追及がうまいこと使うことで楽になる、ログライブラリ log recorder for C# です。
Logフォルダ以下に Log ライブラリ本体（出力は Log.dll）、logcsharp フォルダ以下に Log ライブラリを使ったサンプルプロジェクトがあります。

## ライセンス
Boost Software License 1.0 に準拠します。要するに、保証はないけどご自由にどうぞ。

## 特徴
- FATAL, ERROR, WARNING, INFO, DEBUG, DETAIL の順によるレベル分けができます（コード弄ればお好きなようにできます）。
- スレッドセーフで、全ての出力を一つのファイルにまとめて出力します。
- 時刻は100nsec.まで、出力しているソースコード名、関数名、行数などが自動で付加されます。
- ログのローテートができます。ローテート数、サイズ指定ができますが、サイズは割とアバウトです。指定したサイズを超えるどこかで区切る、みたいな。
- 割とモリモリ出力されても耐えられます。数メガライン/sec.くらいまでは大丈夫かと思います。それ以上になると処理が追いつかず記録漏れが出るケースはあるので、程々に。

## 使い方
ログの情報設定と記録開始は Log.Setup() を呼び出した時点から行います。
アプリケーション終了時に Log.Terminate() を呼び出して終了させてください。
ログの先頭に"Log Start."、ログの最後に"Log Terminated." がFATALレベルで出力されます。
ログの記録は任意の場所で Log.Write[LEVEL]() でどうぞ。文字列を引数に指定するだけです。
