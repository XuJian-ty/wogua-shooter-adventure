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
    public class WoguaShooterUILeaderboardServiceWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(WoguaShooter.UI.LeaderboardService);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 8, 4, 0);
			Utils.RegisterFunc(L, Utils.CLS_IDX, "GetEntries", _m_GetEntries_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "InitializeSaveData", _m_InitializeSaveData_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "Qualifies", _m_Qualifies_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "GetHighestScore", _m_GetHighestScore_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "IsLevelUnlocked", _m_IsLevelUnlocked_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "RecordHighestScore", _m_RecordHighestScore_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "TryAddScore", _m_TryAddScore_xlua_st_);
            
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "MaximumEntries", _g_get_MaximumEntries);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "MaximumNameLength", _g_get_MaximumNameLength);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "ScoreToUnlockNextLevel", _g_get_ScoreToUnlockNextLevel);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "SaveFilePath", _g_get_SaveFilePath);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "WoguaShooter.UI.LeaderboardService does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetEntries_xlua_st_(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
            
                
                {
                    int _levelId = LuaAPI.xlua_tointeger(L, 1);
                    
                        var gen_ret = WoguaShooter.UI.LeaderboardService.GetEntries( _levelId );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_InitializeSaveData_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    
                    WoguaShooter.UI.LeaderboardService.InitializeSaveData(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Qualifies_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    int _levelId = LuaAPI.xlua_tointeger(L, 1);
                    int _score = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = WoguaShooter.UI.LeaderboardService.Qualifies( _levelId, _score );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetHighestScore_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    int _levelId = LuaAPI.xlua_tointeger(L, 1);
                    
                        var gen_ret = WoguaShooter.UI.LeaderboardService.GetHighestScore( _levelId );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_IsLevelUnlocked_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    int _levelId = LuaAPI.xlua_tointeger(L, 1);
                    
                        var gen_ret = WoguaShooter.UI.LeaderboardService.IsLevelUnlocked( _levelId );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RecordHighestScore_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    int _levelId = LuaAPI.xlua_tointeger(L, 1);
                    int _score = LuaAPI.xlua_tointeger(L, 2);
                    
                    WoguaShooter.UI.LeaderboardService.RecordHighestScore( _levelId, _score );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryAddScore_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    int _levelId = LuaAPI.xlua_tointeger(L, 1);
                    string _rawName = LuaAPI.lua_tostring(L, 2);
                    int _score = LuaAPI.xlua_tointeger(L, 3);
                    string _error;
                    
                        var gen_ret = WoguaShooter.UI.LeaderboardService.TryAddScore( _levelId, _rawName, _score, out _error );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    LuaAPI.lua_pushstring(L, _error);
                        
                    
                    
                    
                    return 2;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MaximumEntries(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.xlua_pushinteger(L, WoguaShooter.UI.LeaderboardService.MaximumEntries);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MaximumNameLength(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.xlua_pushinteger(L, WoguaShooter.UI.LeaderboardService.MaximumNameLength);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_ScoreToUnlockNextLevel(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.xlua_pushinteger(L, WoguaShooter.UI.LeaderboardService.ScoreToUnlockNextLevel);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_SaveFilePath(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.lua_pushstring(L, WoguaShooter.UI.LeaderboardService.SaveFilePath);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
