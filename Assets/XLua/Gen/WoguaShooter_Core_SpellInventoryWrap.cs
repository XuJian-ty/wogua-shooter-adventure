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
    public class WoguaShooterCoreSpellInventoryWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.Core.SpellInventory);
			Utils.BeginObjectRegister(type, L, translator, 0, 11, 4, 4);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetSlot", _m_GetSlot);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "TryAdd", _m_TryAdd);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "TryUseCollectedSpell", _m_TryUseCollectedSpell);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "TryAddDetailed", _m_TryAddDetailed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "UseSlot", _m_UseSlot);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "DropSlot", _m_DropSlot);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ResetInventory", _m_ResetInventory);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "FillRandomDistinct", _m_FillRandomDistinct);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Changed", _e_Changed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SlotUsed", _e_SlotUsed);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SlotDropped", _e_SlotDropped);
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "InteractionEnabled", _g_get_InteractionEnabled);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "UseInteractionEnabled", _g_get_UseInteractionEnabled);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "DropInteractionEnabled", _g_get_DropInteractionEnabled);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "MinimumItemCountToPreserveFromUse", _g_get_MinimumItemCountToPreserveFromUse);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "InteractionEnabled", _s_set_InteractionEnabled);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "UseInteractionEnabled", _s_set_UseInteractionEnabled);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "DropInteractionEnabled", _s_set_DropInteractionEnabled);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "MinimumItemCountToPreserveFromUse", _s_set_MinimumItemCountToPreserveFromUse);
            
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 4, 1, 0);
			Utils.RegisterFunc(L, Utils.CLS_IDX, "GetOrCreate", _m_GetOrCreate_xlua_st_);
            
			
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "SlotCount", WoguaShooter.Core.SpellInventory.SlotCount);
            Utils.RegisterObject(L, translator, Utils.CLS_IDX, "MaxStack", WoguaShooter.Core.SpellInventory.MaxStack);
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "Instance", _g_get_Instance);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new WoguaShooter.Core.SpellInventory();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.SpellInventory constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetOrCreate_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
                
                {
                    WoguaShooter.Core.WeaponController _weapon = (WoguaShooter.Core.WeaponController)translator.GetObject(L, 1, typeof(WoguaShooter.Core.WeaponController));
                    
                        var gen_ret = WoguaShooter.Core.SpellInventory.GetOrCreate( _weapon );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetSlot(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _index = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.GetSlot( _index );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryAdd(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WoguaShooter.Config.SpellType _type;translator.Get(L, 2, out _type);
                    
                        var gen_ret = gen_to_be_invoked.TryAdd( _type );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryUseCollectedSpell(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WoguaShooter.Config.SpellType _type;translator.Get(L, 2, out _type);
                    
                        var gen_ret = gen_to_be_invoked.TryUseCollectedSpell( _type );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryAddDetailed(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WoguaShooter.Config.SpellType _type;translator.Get(L, 2, out _type);
                    
                        var gen_ret = gen_to_be_invoked.TryAddDetailed( _type );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_UseSlot(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _index = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.UseSlot( _index );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_DropSlot(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _index = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.DropSlot( _index );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ResetInventory(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ResetInventory(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_FillRandomDistinct(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.FillRandomDistinct(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Instance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, WoguaShooter.Core.SpellInventory.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_InteractionEnabled(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.InteractionEnabled);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_UseInteractionEnabled(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.UseInteractionEnabled);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_DropInteractionEnabled(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.DropInteractionEnabled);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MinimumItemCountToPreserveFromUse(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.MinimumItemCountToPreserveFromUse);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_InteractionEnabled(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.InteractionEnabled = LuaAPI.lua_toboolean(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_UseInteractionEnabled(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.UseInteractionEnabled = LuaAPI.lua_toboolean(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_DropInteractionEnabled(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.DropInteractionEnabled = LuaAPI.lua_toboolean(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_MinimumItemCountToPreserveFromUse(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.MinimumItemCountToPreserveFromUse = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _e_Changed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    int gen_param_count = LuaAPI.lua_gettop(L);
			WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                System.Action gen_delegate = translator.GetDelegate<System.Action>(L, 3);
                if (gen_delegate == null) {
                    return LuaAPI.luaL_error(L, "#3 need System.Action!");
                }
				
				if (gen_param_count == 3)
				{
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "+")) {
						gen_to_be_invoked.Changed += gen_delegate;
						return 0;
					} 
					
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "-")) {
						gen_to_be_invoked.Changed -= gen_delegate;
						return 0;
					} 
					
				}
			} catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
			LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.SpellInventory.Changed!");
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _e_SlotUsed(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    int gen_param_count = LuaAPI.lua_gettop(L);
			WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                System.Action<int, WoguaShooter.Config.SpellType> gen_delegate = translator.GetDelegate<System.Action<int, WoguaShooter.Config.SpellType>>(L, 3);
                if (gen_delegate == null) {
                    return LuaAPI.luaL_error(L, "#3 need System.Action<int, WoguaShooter.Config.SpellType>!");
                }
				
				if (gen_param_count == 3)
				{
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "+")) {
						gen_to_be_invoked.SlotUsed += gen_delegate;
						return 0;
					} 
					
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "-")) {
						gen_to_be_invoked.SlotUsed -= gen_delegate;
						return 0;
					} 
					
				}
			} catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
			LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.SpellInventory.SlotUsed!");
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _e_SlotDropped(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    int gen_param_count = LuaAPI.lua_gettop(L);
			WoguaShooter.Core.SpellInventory gen_to_be_invoked = (WoguaShooter.Core.SpellInventory)translator.FastGetCSObj(L, 1);
                System.Action<int, WoguaShooter.Config.SpellType> gen_delegate = translator.GetDelegate<System.Action<int, WoguaShooter.Config.SpellType>>(L, 3);
                if (gen_delegate == null) {
                    return LuaAPI.luaL_error(L, "#3 need System.Action<int, WoguaShooter.Config.SpellType>!");
                }
				
				if (gen_param_count == 3)
				{
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "+")) {
						gen_to_be_invoked.SlotDropped += gen_delegate;
						return 0;
					} 
					
					
					if (LuaAPI.xlua_is_eq_str(L, 2, "-")) {
						gen_to_be_invoked.SlotDropped -= gen_delegate;
						return 0;
					} 
					
				}
			} catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
			LuaAPI.luaL_error(L, "invalid arguments to WoguaShooter.Core.SpellInventory.SlotDropped!");
            return 0;
        }
        
		
		
    }
}
