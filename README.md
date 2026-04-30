Hey guys, this project is build to online shirt shoppeeeeeee

KaiZhen: Index
YuChen: shirt details
ChengKeat: login logout
RueyEn && XinYee : Admin

Database: Firebase


When u run the code than it will show a problem:
System.InvalidOperationException: 'Error reading credential file from location C:\Users\user\Documents\GitHub\Mini-Project-Assignment_Y2S2\Mini Project Assignment_Y2S2\bin\Debug\net9.0\firebase_config.json: Could not find file 'C:\Users\user\Documents\GitHub\Mini-Project-Assignment_Y2S2\Mini Project Assignment_Y2S2\bin\Debug\net9.0\firebase_config.json'.
Please check the value of the Environment Variable GOOGLE_APPLICATION_CREDENTIALS.'

This problem is because we using firebase to build database. But when we use new pc and clone this code from github, our SDK 密钥（service account key）will not clone. So to slove this problem please follow my step:
1. open firebase, click go console,click your project
2. setting->service account
3. copy your SDK
4. than go to your explorer(your file) and open to this direction :C:\Users\user\Documents\GitHub\Web_Mobile_Assignment_New\Web_Mobile_Assignment_New\bin\Debug\net9.0
5. create a json file(if dont know how to create json file, jump to 6) and name it as firebase_config.json
6. create json file: right click on free space. click "New" -> "Text Document" and name it as firebase_config.json. Remember, window will always not show file name extension(txt), you should open it by yourslef and delete file name extension.
7. Than how to open file name extension? Follow me: click "View" → "File name extensions"
