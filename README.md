# BDODevTest
Development test from BDO.

The program lets you fetch cryptocurrency pairs from twelvedata.com and look up their exchange rates.

# How to use
(0). Open the repo as a solution in Visual Studio
1. Run the program inside the BDODevTest folder at least once (ideally once a day).
2. Run the program inside the UserApplication folder, and follow the instructions.

# Monitoring the database
The cryptocurrency pairs are stored in the Firebase Firestore. 
For monitoring: 
1. Go to https://firebase.google.com/
2. Log in with bdo.dev.test@gmail.com (password: test123abc)
3. Click "Go to console" at the top right of the page.
4. Choose the BDO-Developer-Test project
5. On the left hand side, under "Build", click "Firestore Database"
 NOTE: When the program is writing to the database, you may have to refresh the page in order to see the changes in real time.
