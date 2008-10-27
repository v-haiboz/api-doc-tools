// Part of the mdoc(7) suite of tools.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Options;

namespace Mono.Documentation {

	class MDoc {

		private static bool debug;

		private static void Main (string[] args)
		{
			MDoc d = new MDoc ();
			try {
				d.Run (args);
			}
			catch (Exception e) {
				if (debug) {
					Console.Error.WriteLine ("mdoc: {0}", e.ToString ());
				}
				else {
					Console.Error.WriteLine ("mdoc: {0}", e.Message);
				}
				Console.Error.WriteLine ("See `mdoc help' for more information.");
			}
		}

		int verbosity = 2;

		internal Dictionary<string, MDocCommand> subcommands;

		private void Run (string[] args)
		{
			subcommands = new Dictionary<string, MDocCommand> () {
				{ "assemble",         new MDocAssembler () },
				{ "export-html",      new MDocToHtmlConverter () },
				{ "export-msxdoc",    new MDocToMSXDocConverter () },
				{ "help",             new MDocHelpCommand (this) },
				{ "update",           new MDocUpdater () },
				{ "validate",         new MDocValidator () },
			};

			bool showVersion = false;
			bool showHelp    = false;
			var p = new OptionSet () {
				{ "version",  v => showVersion = v != null },
				{ "v:",       (int? v) => verbosity = v.HasValue ? v.Value : verbosity+1 },
				{ "debug",    v => debug = v != null },
				{ "h|?|help", v => showHelp = v != null },
			};

			List<string> extra = p.Parse (args);

			if (showVersion) {
				Console.WriteLine ("mdoc 0.1.0");
				return;
			}
			if (extra.Count == 0) {
				new MDocHelpCommand (this).Run (null);
			}
			if (showHelp) {
				extra.Add ("--help");
			}
			GetCommand (extra [0]).Run (extra);
		}

		internal MDocCommand GetCommand (string command)
		{
			MDocCommand h;
			if (!subcommands.TryGetValue (command, out h)) {
				Error ("Unknown command: {0}.", command);
			}
			h.TraceLevel  = (TraceLevel) verbosity;
			h.DebugOutput = debug;
			return h;
		}

		private static void Error (string format, params object[] args)
		{
			throw new Exception (string.Format (format, args));
		}
	}

	public abstract class MDocCommand {

		public TraceLevel TraceLevel { get; set; }
		public bool DebugOutput { get; set; }

		public abstract void Run (IEnumerable<string> args);

		protected List<string> Parse (OptionSet p, IEnumerable<string> args, 
				string command, string prototype, string description)
		{
			bool showHelp = false;
			p.Add ("h|?|help", 
					"Show this message and exit.", 
					v => showHelp = v != null );

			List<string> extra = null;
			if (args != null) {
				extra = p.Parse (args.Skip (1));
			}
			if (args == null || showHelp) {
				Console.WriteLine ("usage: mdoc {0} {1}", 
						args == null ? command : args.First(), prototype);
				Console.WriteLine ();
				Console.WriteLine (description);
				Console.WriteLine ();
				Console.WriteLine ("Available Options:");
				p.WriteOptionDescriptions (Console.Out);
				return null;
			}
			return extra;
		}

		public void Error (string format, params object[] args)
		{
			throw new Exception (string.Format (format, args));
		}

		public void Message (TraceLevel level, string format, params object[] args)
		{
			if ((int) level <= (int) TraceLevel)
				Console.WriteLine (format, args);
		}
	}

	class MDocHelpCommand : MDocCommand {

		MDoc instance;

		public MDocHelpCommand (MDoc instance)
		{
			this.instance = instance;
		}

		public override void Run (IEnumerable<string> args)
		{
			if (args != null && args.Count() > 1) {
				foreach (var arg in args.Skip (1)) {
					instance.GetCommand (arg).Run (new string[]{arg, "--help"});
				}
				return;
			}
			Message (TraceLevel.Warning, 
				"usage: mdoc COMMAND [OPTIONS]\n" +
				"Use `mdoc help COMMAND' for help on a specific command.\n" +
				"\n" + 
				"Available commands:\n\n   " +
				string.Join ("\n   ", instance.subcommands.Keys.OrderBy (v => v).ToArray()) +
				"\n\n" + 
				"mdoc is a tool for documentation management.\n" +
				"For additional information, see http://www.mono-project.com/"
			);
		}
	}
}
