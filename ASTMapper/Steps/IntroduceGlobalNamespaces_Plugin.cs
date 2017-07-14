using System.Windows;
using Boo.Lang.Compiler.Steps;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Core;
using Boo.Lang.Environments;

namespace Hill30.Boo.ASTMapper.Steps
{
	internal class IntroduceGlobalNamespaces_Plugin: IntroduceGlobalNamespaces
	{
		public override void Run()
		{
			NameResolutionService.Reset();
			NameResolutionService.GlobalNamespace = new NamespaceDelegator(
										NameResolutionService.GlobalNamespace,
										ThreadSafeGetNamespace("Boo.Lang"),
										ThreadSafeGetNamespace("Boo.Lang.Extensions"),
										TypeSystemServices.BuiltinsType);
		}

		protected INamespace ThreadSafeGetNamespace(string qname)
		{
			INamespace result = null;
			var env = ActiveEnvironment.Instance;
			Application.Current.Dispatcher.Invoke(
				() => ActiveEnvironment.With(env, 
					() => result = SafeGetNamespace(qname)));
			return result;
		}
	}
}

