using System;
using System.Collections.Generic;
using System.Linq;
using Styx.Helpers;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

//public static int cpt = 0;
//public static IEnumerator<string> brute = Program.startBruteForce().GetEnumerator();

//    public static class Program
//    {
//        #region Private variables

//        // the secret password which we will try to find via brute force
//        private static string password = "p12sssssssssssssss3";
//        public static string result;

//        public static bool isMatched = false;

//        /* The length of the charactersToTest Array is stored in a
//         * additional variable to increase performance  */
//        public static int estimatedPasswordLength = 3;
//        public static int charactersToTestLength = 0;
//        public static long computedKeys = 0;

//        /* An array containing the characters which will be used to create the brute force keys,
//         * if less characters are used (e.g. only lower case chars) the faster the password is matched  */

//        public static char[] charactersToTest =
//{
//    'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
//    'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
//    'u', 'v', 'w', 'x', 'y', 'z','A','B','C','D','E',
//    'F','G','H','I','J','K','L','M','N','O','P','Q','R',
//    'S','T','U','V','W','X','Y','Z'
//};

//        #endregion

//        #region Private methods

//        static Program()
//        {
//        }

//        /// <summary>
//        /// Starts the recursive method which will create the keys via brute force
//        /// </summary>
//        /// <param name="keyLength">The length of the key</param>
//        public static IEnumerable<string> startBruteForce()
//        {
//            charactersToTestLength = charactersToTest.Length;


//            while (estimatedPasswordLength < 20)
//            {
//                estimatedPasswordLength++;
//                var keyChars = (from c in new char[estimatedPasswordLength] select charactersToTest[0]).ToArray();
//                // The index of the last character will be stored for slight perfomance improvement
//                var indexOfLastChar = estimatedPasswordLength - 1;
//                foreach (var v in createNewKey(0, keyChars, estimatedPasswordLength, indexOfLastChar))
//                {
//                    yield return v;
//                } 
//            }
//        }

//        /// <summary>
//        /// Creates a new char array of a specific length filled with the defaultChar
//        /// </summary>
//        /// <param name="length">The length of the array</param>
//        /// <param name="defaultChar">The char with whom the array will be filled</param>
//        /// <returns></returns>
//        private static char[] createCharArray(int length, char defaultChar)
//        {
//            return (from c in new char[length] select defaultChar).ToArray();
//        }

//        /// <summary>
//        /// This is the main workhorse, it creates new keys and compares them to the password until the password
//        /// is matched or all keys of the current key length have been checked
//        /// </summary>
//        /// <param name="currentCharPosition">The position of the char which is replaced by new characters currently</param>
//        /// <param name="keyChars">The current key represented as char array</param>
//        /// <param name="keyLength">The length of the key</param>
//        /// <param name="indexOfLastChar">The index of the last character of the key</param>
//        private static IEnumerable<string> createNewKey(int currentCharPosition, char[] keyChars, int keyLength, int indexOfLastChar)
//        {
//            var nextCharPosition = currentCharPosition + 1;
//            // We are looping trough the full length of our charactersToTest array
//            for (int i = 0; i < charactersToTestLength; i++)
//            {
//                /* The character at the currentCharPosition will be replaced by a
//                 * new character from the charactersToTest array => a new key combination will be created */
//                keyChars[currentCharPosition] = charactersToTest[i];

//                // The method calls itself recursively until all positions of the key char array have been replaced
//                if (currentCharPosition < indexOfLastChar)
//                {
//                    foreach (var v in createNewKey(nextCharPosition, keyChars, keyLength, indexOfLastChar))
//                    {
//                        yield return v;
//                    }
//                }
//                else
//                {
//                    // A new key has been created, remove this counter to improve performance
//                    computedKeys++;

//                    /* The char array will be converted to a string and compared to the password. If the password
//                     * is matched the loop breaks and the password is stored as result. */
//                    var test = new String(keyChars);
//                    //Logging.Write("Trying: " + test);
//                    String lua =
//                        "local b = {}; local am = {}; local RetInfo = {}; local cpt = 0; C_Garrison.GetAvailableMissions(am);" +
//                        String.Format("idx = 1;" +
//                                      "b[0] = am[idx].{0};", test) +
//                        "for j_=0,0 do table.insert(RetInfo,tostring(b[j_]));end; " +
//                        "return unpack(RetInfo)";
//                    List<string> ret = Lua.GetReturnValues(lua);
//                    if (ret != null && ret[0] != "nil")
//                        Logging.Write("found: " + test + " | value: " + ret[0]);
//                    yield return test;
//                }
//            }
//        }

//        #endregion
//    }

// Return the mission corresponding to the id