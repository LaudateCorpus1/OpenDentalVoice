﻿using OpenDental;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VoiceCommand {
	public abstract class VoiceCommandAbs {
		protected SpeechRecognitionEngine RecEngine=new SpeechRecognitionEngine();
		protected SpeechSynthesizer Synth=new SpeechSynthesizer();
		protected abstract VoiceCommandArea ProgramArea { get; }
		protected bool IsListening {
			get {
				return _isListening;
			}
			set {
				_isListening=value;
				labelListening.Visible=value;
			}
		}
		protected bool IsGivingFeedback=true;
		private Label labelListening;
		private bool _isListening;
		
		public virtual void InitializeListening() {
			Choices commands=new Choices();
			commands.Add(CommandList.Commands.Where(x => x.Area==ProgramArea || x.Area==VoiceCommandArea.Global)
				.SelectMany(x => x.Commands).ToArray());
			// Create a GrammarBuilder object and append the Choices object.
			GrammarBuilder gb=new GrammarBuilder();
			gb.Append(commands);
			// Create the Grammar instance and load it into the speech recognition engine.
			Grammar g=new Grammar(gb);
			RecEngine=new SpeechRecognitionEngine();
			RecEngine.LoadGrammarAsync(g);
			RecEngine.SetInputToDefaultAudioDevice();
			RecEngine.RecognizeAsync(RecognizeMode.Multiple);
			RecEngine.SpeechRecognized+=RecEngine_SpeechRecognized;
			Synth.SetOutputToDefaultAudioDevice();
			Synth.SelectVoiceByHints(VoiceGender.Female);
		}

		protected virtual void RecEngine_SpeechRecognized(object sender,SpeechRecognizedEventArgs e) {
			VoiceCommand voiceCommand=CommandList.Commands.FirstOrDefault(x => x.Commands.Contains(e.Result.Text));
			if(voiceCommand==null) {
				return;
			}
			if(e.Result.Confidence<0.8) {
				voiceCommand=new VoiceCommand { Action=VoiceCommandAction.DidntGetThat };
			}
			if(voiceCommand.Action==VoiceCommandAction.StartListening) {
				IsListening=true;
			}
			if(voiceCommand.Action==VoiceCommandAction.StopListening) {
				IsListening=false;
			}
			if(!IsListening) {
				return;
			}
			ExecuteVoiceCommand(voiceCommand.Action);
		}

		protected virtual void ExecuteVoiceCommand(VoiceCommandAction action) {
			string response="";
			switch(action) {
				case VoiceCommandAction.GiveFeedback:
					IsGivingFeedback=true;
					response="Giving feedback";
					break;
				case VoiceCommandAction.StopGivingFeedback:
					IsGivingFeedback=false;
					response="No longer giving feedback";
					Synth.Speak(response);
					break;
				case VoiceCommandAction.DidntGetThat:
					response="I didn't get that.";
					break;
			}
			SayResponse(response);
		}

		protected void SayResponse(string response,int pauseBefore=0) {
			if(!string.IsNullOrEmpty(response) && IsGivingFeedback) {
				Thread.Sleep(pauseBefore);
				_isListening=false;
				Synth.Speak(response);
				_isListening=true;
			}
		}

		protected virtual void AddMicButton(Control control,Point point) {
			OpenDental.UI.Button butMic=new OpenDental.UI.Button();
			butMic.Image=Properties.Resources.Mic_30px;
			butMic.ImageAlign=ContentAlignment.MiddleCenter;
			butMic.Location=point;
			butMic.Size=new Size(34,34);
			butMic.Click+=butMic_Click;
			control.Controls.Add(butMic);
			labelListening=new Label();
			labelListening.Text="Listening";
			labelListening.ForeColor=Color.LimeGreen;
			labelListening.Location=new Point(point.X+36,point.Y+10);
			control.Controls.Add(labelListening);
		}

		protected void butMic_Click(object sender,EventArgs e) {
			IsListening=!IsListening;
		}
	}
}