//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
// Developed by Minigraph
//
// Author:  James Stanard 
//
#include "Common.h"
#include "Utility.h"
#include <string>


HRESULT WINAPI DXTraceW(_In_z_ const WCHAR* strFile, _In_ DWORD dwLine, _In_ HRESULT hr,
	_In_opt_ const WCHAR* strMsg, _In_ bool bPopMsgBox)
{
	WCHAR strBufferFile[MAX_PATH];
	WCHAR strBufferLine[128];
	WCHAR strBufferError[300];
	WCHAR strBufferMsg[1024];
	WCHAR strBufferHR[40];
	WCHAR strBuffer[3000];

	swprintf_s(strBufferLine, 128, L"%lu", dwLine);
	if (strFile)
	{
		swprintf_s(strBuffer, 3000, L"%ls(%ls): ", strFile, strBufferLine);
		OutputDebugStringW(strBuffer);
	}

	size_t nMsgLen = (strMsg) ? wcsnlen_s(strMsg, 1024) : 0;
	if (nMsgLen > 0)
	{
		OutputDebugStringW(strMsg);
		OutputDebugStringW(L" ");
	}
	// Windows SDK 8.0起DirectX的错误信息已经集成进错误码中，可以通过FormatMessageW获取错误信息字符串
	// 不需要分配字符串内存
	FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		nullptr, hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		strBufferError, 256, nullptr);

	WCHAR* errorStr = wcsrchr(strBufferError, L'\r');
	if (errorStr)
	{
		errorStr[0] = L'\0';	// 擦除FormatMessageW带来的换行符(把\r\n的\r置换为\0即可)
	}

	swprintf_s(strBufferHR, 40, L" (0x%0.8x)", hr);
	wcscat_s(strBufferError, strBufferHR);
	swprintf_s(strBuffer, 3000, L"错误码含义：%ls", strBufferError);
	OutputDebugStringW(strBuffer);

	OutputDebugStringW(L"\n");

	if (bPopMsgBox)
	{
		wcscpy_s(strBufferFile, MAX_PATH, L"");
		if (strFile)
			wcscpy_s(strBufferFile, MAX_PATH, strFile);

		wcscpy_s(strBufferMsg, 1024, L"");
		if (nMsgLen > 0)
			swprintf_s(strBufferMsg, 1024, L"当前调用：%ls\n", strMsg);

		swprintf_s(strBuffer, 3000, L"文件名：%ls\n行号：%ls\n错误码含义：%ls\n%ls您需要调试当前应用程序吗？",
			strBufferFile, strBufferLine, strBufferError, strBufferMsg);

		int nResult = MessageBoxW(GetForegroundWindow(), strBuffer, L"错误", MB_YESNO | MB_ICONERROR);
		if (nResult == IDYES)
			DebugBreak();
	}

	return hr;
}

// A faster version of memcopy that uses SSE instructions.  TODO:  Write an ARM variant if necessary.
void SIMDMemCopy( void* __restrict _Dest, const void* __restrict _Source, size_t NumQuadwords )
{
    ASSERT(Math::IsAligned(_Dest, 16));
    ASSERT(Math::IsAligned(_Source, 16));

    __m128i* __restrict Dest = (__m128i* __restrict)_Dest;
    const __m128i* __restrict Source = (const __m128i* __restrict)_Source;

    // Discover how many quadwords precede a cache line boundary.  Copy them separately.
    size_t InitialQuadwordCount = (4 - ((size_t)Source >> 4) & 3) & 3;
    if (InitialQuadwordCount > NumQuadwords)
        InitialQuadwordCount = NumQuadwords;

    switch (InitialQuadwordCount)
    {
    case 3: _mm_stream_si128(Dest + 2, _mm_load_si128(Source + 2));     // Fall through
    case 2: _mm_stream_si128(Dest + 1, _mm_load_si128(Source + 1));     // Fall through
    case 1: _mm_stream_si128(Dest + 0, _mm_load_si128(Source + 0));     // Fall through
    default:
        break;
    }

    if (NumQuadwords == InitialQuadwordCount)
        return;

    Dest += InitialQuadwordCount;
    Source += InitialQuadwordCount;
    NumQuadwords -= InitialQuadwordCount;

    size_t CacheLines = NumQuadwords >> 2;

    switch (CacheLines)
    {
    default:
    case 10: _mm_prefetch((char*)(Source + 36), _MM_HINT_NTA);    // Fall through
    case 9:  _mm_prefetch((char*)(Source + 32), _MM_HINT_NTA);    // Fall through
    case 8:  _mm_prefetch((char*)(Source + 28), _MM_HINT_NTA);    // Fall through
    case 7:  _mm_prefetch((char*)(Source + 24), _MM_HINT_NTA);    // Fall through
    case 6:  _mm_prefetch((char*)(Source + 20), _MM_HINT_NTA);    // Fall through
    case 5:  _mm_prefetch((char*)(Source + 16), _MM_HINT_NTA);    // Fall through
    case 4:  _mm_prefetch((char*)(Source + 12), _MM_HINT_NTA);    // Fall through
    case 3:  _mm_prefetch((char*)(Source + 8 ), _MM_HINT_NTA);    // Fall through
    case 2:  _mm_prefetch((char*)(Source + 4 ), _MM_HINT_NTA);    // Fall through
    case 1:  _mm_prefetch((char*)(Source + 0 ), _MM_HINT_NTA);    // Fall through

        // Do four quadwords per loop to minimize stalls.
        for (size_t i = CacheLines; i > 0; --i)
        {
            // If this is a large copy, start prefetching future cache lines.  This also prefetches the
            // trailing quadwords that are not part of a whole cache line.
            if (i >= 10)
                _mm_prefetch((char*)(Source + 40), _MM_HINT_NTA);

            _mm_stream_si128(Dest + 0, _mm_load_si128(Source + 0));
            _mm_stream_si128(Dest + 1, _mm_load_si128(Source + 1));
            _mm_stream_si128(Dest + 2, _mm_load_si128(Source + 2));
            _mm_stream_si128(Dest + 3, _mm_load_si128(Source + 3));

            Dest += 4;
            Source += 4;
        }

    case 0:    // No whole cache lines to read
        break;
    }

    // Copy the remaining quadwords
    switch (NumQuadwords & 3)
    {
    case 3: _mm_stream_si128(Dest + 2, _mm_load_si128(Source + 2));     // Fall through
    case 2: _mm_stream_si128(Dest + 1, _mm_load_si128(Source + 1));     // Fall through
    case 1: _mm_stream_si128(Dest + 0, _mm_load_si128(Source + 0));     // Fall through
    default:
        break;
    }

    _mm_sfence();
}

void SIMDMemFill( void* __restrict _Dest, __m128 FillVector, size_t NumQuadwords )
{
    ASSERT(Math::IsAligned(_Dest, 16));

    register const __m128i Source = _mm_castps_si128(FillVector);
    __m128i* __restrict Dest = (__m128i* __restrict)_Dest;

    switch (((size_t)Dest >> 4) & 3)
    {
    case 1: _mm_stream_si128(Dest++, Source); --NumQuadwords;     // Fall through
    case 2: _mm_stream_si128(Dest++, Source); --NumQuadwords;     // Fall through
    case 3: _mm_stream_si128(Dest++, Source); --NumQuadwords;     // Fall through
    default:
        break;
    }

    size_t WholeCacheLines = NumQuadwords >> 2;

    // Do four quadwords per loop to minimize stalls.
    while (WholeCacheLines--)
    {
        _mm_stream_si128(Dest++, Source);
        _mm_stream_si128(Dest++, Source);
        _mm_stream_si128(Dest++, Source);
        _mm_stream_si128(Dest++, Source);
    }

    // Copy the remaining quadwords
    switch (NumQuadwords & 3)
    {
    case 3: _mm_stream_si128(Dest++, Source);     // Fall through
    case 2: _mm_stream_si128(Dest++, Source);     // Fall through
    case 1: _mm_stream_si128(Dest++, Source);     // Fall through
    default:
        break;
    }

    _mm_sfence();
}

std::wstring MakeWStr( const std::string& str )
{
    return std::wstring(str.begin(), str.end());
}
