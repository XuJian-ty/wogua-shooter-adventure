package com.wogua.shooter;

import android.app.Activity;
import android.content.Context;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputMethodManager;
import android.widget.EditText;

import com.unity3d.player.UnityPlayer;

import java.lang.reflect.Field;

/** Keeps Unity's native text editor out of Android IME extract/fullscreen mode. */
public final class WoguaShooterKeyboardBridge {
    private static final int MAX_ATTEMPTS = 20;
    private static final long RETRY_DELAY_MS = 50L;

    private WoguaShooterKeyboardBridge() {
    }

    public static void configure(final boolean keyboardVisible) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Window window = activity.getWindow();
                window.setSoftInputMode(
                        android.view.WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_HIDDEN
                                | android.view.WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE);

                if (!keyboardVisible) {
                    return;
                }

                new Handler(Looper.getMainLooper()).post(new Runnable() {
                    private int attempts;

                    @Override
                    public void run() {
                        EditText editor = findUnityEditor(activity);
                        if (editor != null) {
                            applyImeFlags(activity, editor);
                            return;
                        }

                        if (++attempts < MAX_ATTEMPTS) {
                            new Handler(Looper.getMainLooper()).postDelayed(this, RETRY_DELAY_MS);
                        }
                    }
                });
            }
        });
    }

    private static void applyImeFlags(Activity activity, EditText editor) {
        int options = editor.getImeOptions();
        options |= EditorInfo.IME_FLAG_NO_EXTRACT_UI;
        options |= EditorInfo.IME_FLAG_NO_FULLSCREEN;
        editor.setImeOptions(options);
        editor.setSingleLine(true);

        InputMethodManager inputMethodManager =
                (InputMethodManager) activity.getSystemService(Context.INPUT_METHOD_SERVICE);
        if (inputMethodManager != null) {
            inputMethodManager.restartInput(editor);
        }
    }

    private static EditText findUnityEditor(Activity activity) {
        try {
            Field unityPlayerField = findField(activity.getClass(), "mUnityPlayer");
            if (unityPlayerField != null) {
                unityPlayerField.setAccessible(true);
                Object unityPlayer = unityPlayerField.get(activity);
                if (unityPlayer != null) {
                    Field softInputField = findField(unityPlayer.getClass(), "mSoftInput");
                    if (softInputField != null) {
                        softInputField.setAccessible(true);
                        Object softInput = softInputField.get(unityPlayer);
                        if (softInput != null) {
                            Field editorField = findField(softInput.getClass(), "c");
                            if (editorField != null) {
                                editorField.setAccessible(true);
                                Object editor = editorField.get(softInput);
                                if (editor instanceof EditText) {
                                    return (EditText) editor;
                                }
                            }
                        }
                    }
                }
            }
        } catch (Exception ignored) {
            // Fall through to the view hierarchy for Unity versions with different internals.
        }

        return findEditText(activity.getWindow().getDecorView());
    }

    private static EditText findEditText(View view) {
        if (view instanceof EditText) {
            return (EditText) view;
        }
        if (!(view instanceof ViewGroup)) {
            return null;
        }

        ViewGroup group = (ViewGroup) view;
        for (int i = 0; i < group.getChildCount(); i++) {
            EditText result = findEditText(group.getChildAt(i));
            if (result != null) {
                return result;
            }
        }
        return null;
    }

    private static Field findField(Class<?> type, String name) {
        Class<?> current = type;
        while (current != null) {
            try {
                return current.getDeclaredField(name);
            } catch (NoSuchFieldException ignored) {
                current = current.getSuperclass();
            }
        }
        return null;
    }
}
