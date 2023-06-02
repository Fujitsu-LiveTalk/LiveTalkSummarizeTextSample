# LiveTalkSummarizeTextSample
LiveTalk から出力した CSV ファイルを読み込んで、 Azure Cognitive Services for Language の Document summarization (preview) を使って CSV ファイルに記録された発言内容の要約を表示するサンプルです。  
本サンプルコードは、 .NET 7.0 で作成しています。コードレベルでは .NET Framework 4.6 と互換性があります。

ドキュメント要約には、ドキュメント内の重要な文を抽出する「抽出要約」と、主要な情報をドキュメントから生成する「抽象要約」があります。
今回は、「抽出要約」を実施します。

# 補足
2023/06/01時点では、Azure Cognitive Services for Language の Document summarization (preview) は、OpenAIによる文章要約ではありません。

Azure OpenAI による文章要約については、別サンプルとして公開を予定しています。

# 事前準備
1. Azure Cognitive Services for Language の Document summarization (preview) を利用できるように Azure Portal にサインインして設定を行います。そのためには、有効な Azure アカウントにサインイン、または、無料アカウントを作成して Azure Portal にサインインします。
2. [リソースの作成] の検索機能で「Cognitive Services」を [作成] をクリックします。
3. [リージョン] には、「Japan East」を指定します。

   * なお、 2023/06/01 現時点で「Abstractive summarization (抽象要約)」を使用する場合は、提供されているリージョンが限定されているため、リージョンには必ず「East US」を指定する必要があります。

4. [リソースグループ] 、 [名前] 、 [価格レベル] を指定して、 [確認と作成] をクリックします。
5. 表示された内容に問題がなければ、 [作成] をクリックします。
6. 作成が完了したら、該当リソースに移動します。 [キーとエンドポイント] メニューにより「キー」と「エンドポイント」を表示し、それぞれメモしておきます。
7. メモした「キー」と「エンドポイント」は、 App.config ファイルの「APIKey」と「APIResourceName」に設定します。

# サンプルコードの動き
本サンプルでは、LiveTalkを使って会議での発話を音声認識した結果を会議後に保存した CSV ファイルを入力して、全体の行数に対して指定されたパーセントに相当する行を抽出します。

そうすることで会議での重要な発言を抽出して、会議の概要要約になることを想定しています。

サンプルコード動作を簡単に説明すると次のような動作をします。  
1. [ファイル]-[開く] メニューにて、LiveTalkから出力した CSV ファイルを指定します。指定したファイル名は、 [連携ファイル] 欄に表示されます。
要約率を1～100で指定します。CSV ファイルの行数 x 要約率 % が出力される行数となります。
2. [Start] ボタンをクリックすると、 SummarizeModel クラスの中で次のように API を呼び出します。

   1. 指定されたファイル名から発言内容を取り出します。
   2. JSON形式でリクエスト本文を作成します。
   3. 作成したリクエスト本文を https://{0}.cognitiveservices.azure.com/text/analytics/v3.2-preview.2/analyze に http-Post します。「{0}」には App.Config に指定した「エンドポイント」を設定します。
   4. POST した結果として、返却用ロケーションを取得します。
   5. 返却用ロケーションを指定して http-Get します。
   6. GET した結果の completed が 1 になるまで 1000ms ごとに 5 を繰り返します。
   7. completed が 1 のときは、sentences の内容を返却された要素数分だけ連結して「抽出要約」とします。

3. 取得した「抽出要約」を [結果] 欄に表示します。この表示欄の内容を選択してクリップボードにコピーできるます。

# 連絡事項
本ソースコードは、LiveTalkの保守サポート範囲に含まれません。  
頂いたissueについては、必ずしも返信できない場合があります。  
LiveTalkそのものに関するご質問は、公式WEBサイトのお問い合わせ窓口からご連絡ください。
