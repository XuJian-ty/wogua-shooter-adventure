#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class WoguaShooterUITutorialGuideControllerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.UI.TutorialGuideController);
			Utils.BeginObjectRegister(type, L, translator, 0, 3, 0, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Initialize", _m_Initialize);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "BeginFirstLevel", _m_BeginFirstLevel);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "StopTutorial", _m_StopTutorial);
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 0, 0);
			
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new WoguaShooter.UI.TutorialGuideController();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.UI.TutorialGuideController constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Initialize(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.UI.TutorialGuideController gen_to_be_invoked = (WoguaShooter.UI.TutorialGuideController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Canvas _targetCanvas = (UnityEngine.Canvas)translator.GetObject(L, 2, typeof(UnityEngine.Canvas));
                    UnityEngine.Font _font = (UnityEngine.Font)translator.GetObject(L, 3, typeof(UnityEngine.Font));
                    
                    gen_to_be_invoked.Initialize( _targetCanvas, _font );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_BeginFirstLevel(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.UI.TutorialGuideController gen_to_be_invoked = (WoguaShooter.UI.TutorialGuideController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WoguaShooter.Core.PlayerController _targetPlayer = (WoguaShooter.Core.PlayerController)translator.GetObject(L, 2, typeof(WoguaShooter.Core.PlayerController));
                    WoguaShooter.Core.SpellInventory _targetInventory = (WoguaShooter.Core.SpellInventory)translator.GetObject(L, 3, typeof(WoguaShooter.Core.SpellInventory));
                    WoguaShooter.Core.SpawnManager _targetSpawner = (WoguaShooter.Core.SpawnManager)translator.GetObject(L, 4, typeof(WoguaShooter.Core.SpawnManager));
                    
                    gen_to_be_invoked.BeginFirstLevel( _targetPlayer, _targetInventory, _targetSpawner );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_StopTutorial(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.UI.TutorialGuideController gen_to_be_invoked = (WoguaShooter.UI.TutorialGuideController)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.StopTutorial(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        
        
		
		
		
		
    }
}
