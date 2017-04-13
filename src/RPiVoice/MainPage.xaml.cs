using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using RPiSpeech;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RPiVoice
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Grammer File
        private const string SRGS_FILE = "Grammar\\grammar.xml";

        private const string TAG_CMD = "cmd";
        // Speech Recognizer
        private SpeechRecognizer _musicPlaybackRecognizer;
        private SpeechRecognizer _commadRecognizer;
        private SpeechRecognizer _webserachRecognizer;
        private MediaPlayer _player;

        public MainPage()
        {
            this.InitializeComponent();
            _player = new Windows.Media.Playback.MediaPlayer();
            Unloaded += MainPage_Unloaded;
            // Initialize Recognizer
            initializeSpeechRecognizer();
           
        }

        

        // Release resources, stop _musicPlaybackrecognizer, release pins, etc...
        private async void MainPage_Unloaded(object sender, object args)
        {
            // Stop recognizing
            await _musicPlaybackRecognizer.ContinuousRecognitionSession.StopAsync();
            await _commadRecognizer.ContinuousRecognitionSession.StopAsync();

            _commadRecognizer.Dispose();
            
            _musicPlaybackRecognizer.Dispose();

            _musicPlaybackRecognizer = null;
        }

        // Initialize Speech Recognizer and start async recognition
        private async void initializeSpeechRecognizer()
        {
            await MusicPlaybackInitilize();
            await CommandPlaybackInitilize();
            await InitializeWebSearch();

            //_musicPlaybackRecognizer.UIOptions.AudiblePrompt = "Say what you want to search for...";
            //_musicPlaybackRecognizer.UIOptions.ExampleText = @"Ex. &#39;weather for London&#39;";

            // Compile the constraint.
            //await _musicPlaybackrecognizer.CompileConstraintsAsync();
        }

        private async Task CommandPlaybackInitilize()
        {
            _commadRecognizer = new SpeechRecognizer();


            _commadRecognizer.ContinuousRecognitionSession.ResultGenerated += CommandRecognized;

            // Add to grammer constraint
            _commadRecognizer.Constraints.Add(new SpeechRecognitionListConstraint(new List<string> {"Alexa"}));

            await StartContinuousListening(_commadRecognizer);
        }

        private async void CommandRecognized(SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // Output debug strings
            Debug.WriteLine("status : " + args.Result.Status);
            Debug.WriteLine("Text : " + args.Result.Text);

            if (args.Result.Text.Equals("Alexa",StringComparison.CurrentCultureIgnoreCase) || (args.Result.Status == SpeechRecognitionResultStatus.Success &&
                (args.Result.Confidence == SpeechRecognitionConfidence.High ||
                 args.Result.Confidence == SpeechRecognitionConfidence.Medium)))
            {
                //Initilize conversation 
               TextToSpeech.Speak("Hello human how can I help you");
                //Task.Delay(2000).Wait();
                //using (var webserachRecognizer = new SpeechRecognizer())
                //{
                    // Add a web search grammar to the _musicPlaybackrecognizer.

                try
                {
                   
                    var itemRecognition = await _webserachRecognizer.RecognizeWithUIAsync();

                    WebSearchCompeted(itemRecognition);

                }
                catch (Exception e)
                {
                   Debug.WriteLine(e);
                }
                //}

            }
        }

        private async Task InitializeWebSearch()
        {
            _webserachRecognizer = new SpeechRecognizer();

            var webSearchGrammar =
                new SpeechRecognitionTopicConstraint(
                    SpeechRecognitionScenario.WebSearch,
                    "webSearch");
            //_webserachRecognizer.ContinuousRecognitionSession.ResultGenerated += ContinuousRecognitionSession_ResultGenerated;

            _webserachRecognizer.Constraints.Add(webSearchGrammar);
            // await StartContinuousListening(_webserachRecognizer);

            SpeechRecognitionCompilationResult compilationResult = await _webserachRecognizer.CompileConstraintsAsync();

            Debug.WriteLine("Status: " + compilationResult.Status);


        }

        private void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {

            // Output debug strings
            Debug.WriteLine("status : " + args.Result.Status);
            Debug.WriteLine("Text : " + args.Result.Text);

            int count = args.Result.SemanticInterpretation.Properties.Count;

            Debug.WriteLine("Count: " + count);
            Debug.WriteLine("Tag: " + args.Result.Constraint.Tag);
        }

        private void WebSearchCompeted(SpeechRecognitionResult results)
        {
            if (results.Status != SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Sorry, I wasn't able to hear you. Try again later.");
                return;
            }

            Debug.WriteLine("Text : " + results.Text);

        }

        private async Task MusicPlaybackInitilize()
        {
            _musicPlaybackRecognizer = new SpeechRecognizer();

            // Set event handlers
            _musicPlaybackRecognizer.StateChanged += MusicPlaybackRecognizerStateChanged;
            _musicPlaybackRecognizer.ContinuousRecognitionSession.ResultGenerated += MusicResultGenerated;

            // Load Grammer file constraint
            string fileName = String.Format(SRGS_FILE);
            StorageFile grammarContentFile = await Package.Current.InstalledLocation.GetFileAsync(fileName);

            SpeechRecognitionGrammarFileConstraint grammarConstraint =
                new SpeechRecognitionGrammarFileConstraint(grammarContentFile);


            _musicPlaybackRecognizer.Constraints.Add(grammarConstraint);

            // Compile grammer
            await StartContinuousListening(_musicPlaybackRecognizer);
        }

        private async Task StartContinuousListening(SpeechRecognizer recognizer)
        {
            SpeechRecognitionCompilationResult compilationResult = await recognizer.CompileConstraintsAsync();

            Debug.WriteLine("Status: " + compilationResult.Status);

            // If successful, display the recognition result.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Result: " + compilationResult);
                await recognizer.ContinuousRecognitionSession.StartAsync();
            }
            else
            {
                Debug.WriteLine("Status: " + compilationResult.Status);
            }
        }

        // Recognizer generated results
        private void MusicResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // Output debug strings
            Debug.WriteLine("status : " + args.Result.Status);
            Debug.WriteLine("Text : " + args.Result.Text);

            int count = args.Result.SemanticInterpretation.Properties.Count;

            Debug.WriteLine("Count: " + count);
            Debug.WriteLine("Tag: " + args.Result.Constraint.Tag);

           
            // Check for different tags and initialize the variables
//            String target = args.Result.SemanticInterpretation.Properties.ContainsKey(TAG_TARGET) ?
//                            args.Result.SemanticInterpretation.Properties[TAG_TARGET][0].ToString() :
//                            "";

            String cmd = args.Result.SemanticInterpretation.Properties.ContainsKey(TAG_CMD) ?
                            args.Result.SemanticInterpretation.Properties[TAG_CMD][0].ToString() :
                            "";

//            String device = args.Result.SemanticInterpretation.Properties.ContainsKey(TAG_DEVICE) ?
//                            args.Result.SemanticInterpretation.Properties[TAG_DEVICE][0].ToString() :
//                            "";


            Debug.WriteLine("Command: " + cmd );

            if (cmd.Equals("play",StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    _player.Pause();
                    _player.Source = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("https://universalstorage232.blob.core.windows.net/music/Demons.mp3")));

                    _player.Play();

                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }
            else if(cmd.Equals("pause",StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    _player.Pause();

                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }
            else if (cmd.Equals("stop", StringComparison.CurrentCultureIgnoreCase))
            {

                try
                {
                    _player.Pause();

                }
                catch (Exception e)
                {
                    Debug.Write(e);
                }
            }

         
        }

        // Recognizer state changed
        private void MusicPlaybackRecognizerStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech _musicPlaybackrecognizer state: " +  args.State.ToString());
        }
        
    }
}