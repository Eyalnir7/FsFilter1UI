#include <fltkernel.h>
#include <dontuse.h>
#include <suppress.h>
#include <ntddk.h>

//Folder values in registry needs to be all CAPS!!!!

PFLT_FILTER	FilterHandle = NULL;
//if lock = 1 the callbacks won't do anything
int lock = 1;
//assuming the max name length is 50
wchar_t* FolderName;
//UNICODE_STRING ValueName;
//WCHAR name = L"FileName";
//RtlInitUnicodeString(&ValueName,name);
HANDLE OpenRegistryKey(PUNICODE_STRING RegistryPath);
NTSTATUS MiniUnload(FLT_FILTER_UNLOAD_FLAGS Flags);
//FLT_POSTOP_CALLBACK_STATUS MiniPostCreate(PFLT_CALLBACK_DATA Data, PCFLT_RELATED_OBJECTS FltObjects, PVOID* CompletionContext, FLT_POST_OPERATION_FLAGS Flags);
FLT_PREOP_CALLBACK_STATUS MiniPreCreate(PFLT_CALLBACK_DATA Data, PCFLT_RELATED_OBJECTS FltObjects, PVOID* CompletionContext);
FLT_PREOP_CALLBACK_STATUS MiniPreWrite(PFLT_CALLBACK_DATA Data, PCFLT_RELATED_OBJECTS FltObjects, PVOID* CompletionContext);
//go through the array and checks if one of the strings in the array is in str.
//return 0 for false
int CheckString(wchar_t* str, wchar_t* strarr);

const FLT_OPERATION_REGISTRATION Callbacks[] = {
	{IRP_MJ_CREATE,0,MiniPreCreate,NULL},
	{IRP_MJ_WRITE,0,MiniPreWrite,NULL},
	{IRP_MJ_OPERATION_END}
};

const FLT_REGISTRATION FilterRegistration = {
	sizeof(FLT_REGISTRATION),
	FLT_REGISTRATION_VERSION,
	0,
	NULL,
	Callbacks,
	MiniUnload,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL,
	NULL
};

NTSTATUS MiniUnload(FLT_FILTER_UNLOAD_FLAGS Flags) {
	DbgPrint("dbgprint - noder");
	KdPrint(("kdPrint - noder"));
	KdPrint(("driver unload \r\n"));
	ExFreePool(FolderName);
	FltUnregisterFilter(FilterHandle);

	return STATUS_SUCCESS;
}

//FLT_POSTOP_CALLBACK_STATUS MiniPostCreate(PFLT_CALLBACK_DATA Data, PCFLT_RELATED_OBJECTS FltObjects, PVOID* CompletionContext, FLT_POST_OPERATION_FLAGS Flags)
//{
	//KdPrint(("post create is running \r\n"));
//	return FLT_POSTOP_FINISHED_PROCESSING;
//}

FLT_PREOP_CALLBACK_STATUS MiniPreCreate(PFLT_CALLBACK_DATA Data, PCFLT_RELATED_OBJECTS FltObjects, PVOID* CompletionContext)
{
	if (lock != 1) {
		PFLT_FILE_NAME_INFORMATION FileNameInfo;
		NTSTATUS status;
		WCHAR Name[200] = { 0 };
		status = FltGetFileNameInformation(Data, FLT_FILE_NAME_NORMALIZED | FLT_FILE_NAME_QUERY_DEFAULT, &FileNameInfo);
		if (NT_SUCCESS(status)) {
			status = FltParseFileNameInformation(FileNameInfo);
			if (NT_SUCCESS(status)) {
				if (FileNameInfo->Name.MaximumLength < 200) {
					RtlCopyMemory(Name, FileNameInfo->Name.Buffer, FileNameInfo->Name.MaximumLength);
					_wcsupr(Name);
					if (CheckString(Name, FolderName) == 1) {
						Data->IoStatus.Status = STATUS_INVALID_PARAMETER;
						Data->IoStatus.Information = 0;
						FltReleaseFileNameInformation(FileNameInfo);
						return FLT_PREOP_COMPLETE;
					}
				}
			}

			FltReleaseFileNameInformation(FileNameInfo);
		}
	}
	return FLT_PREOP_SUCCESS_NO_CALLBACK;
}

FLT_PREOP_CALLBACK_STATUS MiniPreWrite(PFLT_CALLBACK_DATA Data, PCFLT_RELATED_OBJECTS FltObjects, PVOID* CompletionContext)
{
	if (lock != 1) {
		PFLT_FILE_NAME_INFORMATION FileNameInfo;
		NTSTATUS status;
		WCHAR Name[200] = { 0 };
		status = FltGetFileNameInformation(Data, FLT_FILE_NAME_NORMALIZED | FLT_FILE_NAME_QUERY_DEFAULT, &FileNameInfo);
		if (NT_SUCCESS(status)) {
			status = FltParseFileNameInformation(FileNameInfo);
			if (NT_SUCCESS(status)) {
				if (FileNameInfo->Name.MaximumLength < 200) {
					RtlCopyMemory(Name, FileNameInfo->Name.Buffer, FileNameInfo->Name.MaximumLength);
					_wcsupr(Name);
					//if (wcsstr(Name, L"NODER")!=NULL) {
					DbgPrint("The name of the file is %S", Name);
					if (CheckString(Name, FolderName) == 1) {
						//KdPrint(("prewrite file: %ws blocked \r\n", Name));
						Data->IoStatus.Status = STATUS_INVALID_PARAMETER;
						Data->IoStatus.Information = 0;
						FltReleaseFileNameInformation(FileNameInfo);
						return FLT_PREOP_COMPLETE;
					}
					KdPrint(("prewrite file: %ws \r\n", Name));
				}
			}

			FltReleaseFileNameInformation(FileNameInfo);
		}
	}
	return FLT_PREOP_SUCCESS_NO_CALLBACK;
}

//strarr is a string with many strings in it separated by \0, str is a string
//Folder values in registry needs to be all CAPS!!!!
int CheckString(wchar_t* requestedPath, wchar_t* strarr)
{
	int i = 0;
	int j = 0;
	while (!(*(strarr + i) == L'\0'))
	{
		while (*(strarr + i) != L'\0' && *(requestedPath + j) != L'\0')
		{
			if (requestedPath[j] != strarr[i])
			{
				j = 0;
				break;
			}
			i++;
			j++;
		}
		if (*(strarr + i) == L'\0')
		{
			// if we reached the end of both strings they are the same
			// 0x005c is unicode for back slash
			if (*(requestedPath + j) == L'\0' || *(requestedPath + j) == 0x005C)
				return TRUE; //return true
		}
		while (!(*(strarr + i) == L'\0'))
		{
			i++;
		}
		i++;
	}
	return FALSE;
}

HANDLE OpenRegistryKey(PUNICODE_STRING RegistryPath)

{

	NTSTATUS ntStatus;

	HANDLE hKey;

	OBJECT_ATTRIBUTES objAttr;

	InitializeObjectAttributes(&objAttr, RegistryPath, OBJ_KERNEL_HANDLE | OBJ_CASE_INSENSITIVE, NULL, NULL);

	ntStatus = ZwOpenKey(&hKey, STANDARD_RIGHTS_ALL, &objAttr);


	if (ntStatus != STATUS_SUCCESS)

	{

		return NULL;

	}

	return hKey;

}
//Folder values in registry needs to be all CAPS!!!!
// registry path should be Computer\HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FsFilter1
NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath)
{
	NTSTATUS status;
	PKEY_VALUE_PARTIAL_INFORMATION  pKeyInfo = NULL;
	UNICODE_STRING               ValueName;
	ULONG                        ulKeyInfoSize = 0;
	ULONG                        ulKeyInfoSizeNeeded = 0;
	DbgPrint("noder");
	KdPrint(("Driver Entry noder"));
	KdPrint(("Unicode string: %wZ\n", RegistryPath));
	status = FltRegisterFilter(DriverObject, &FilterRegistration, &FilterHandle);
	if (NT_SUCCESS(status)) {
		status = FltStartFiltering(FilterHandle);
		if (!NT_SUCCESS(status)) {
			FltUnregisterFilter(FilterHandle);
			return status;
		}
		HANDLE handleRegKey = OpenRegistryKey(RegistryPath);
		if (handleRegKey != NULL) {
			RtlInitUnicodeString(&ValueName, L"BlockFolder");
			status = ZwQueryValueKey(handleRegKey,
				&ValueName,
				KeyValuePartialInformation,
				pKeyInfo,
				ulKeyInfoSize,
				&ulKeyInfoSizeNeeded);
			// The driver expects one of the following errors.
			if ((status == STATUS_BUFFER_TOO_SMALL) || (status == STATUS_BUFFER_OVERFLOW))
			{
				// Allocate the memory required for the key.
				ulKeyInfoSize = ulKeyInfoSizeNeeded;
				pKeyInfo = (PKEY_VALUE_PARTIAL_INFORMATION)ExAllocatePool(NonPagedPool, ulKeyInfoSizeNeeded);
				if (NULL == pKeyInfo)
				{
					FltUnregisterFilter(FilterHandle);
					return status;
				}
				RtlZeroMemory(pKeyInfo, ulKeyInfoSize);
				status = ZwQueryValueKey(handleRegKey,
					&ValueName,
					KeyValuePartialInformation,
					pKeyInfo,
					ulKeyInfoSize,
					&ulKeyInfoSizeNeeded);
				if ((status != STATUS_SUCCESS) || (ulKeyInfoSizeNeeded != ulKeyInfoSize) || (NULL == pKeyInfo))
				{
					FltUnregisterFilter(FilterHandle);
					return status;
				}

				//DbgPrint("att: %S", (pKeyInfo->Type*)(pKeyInfo+pKeyInfo->DataOffset));
				//DbgPrint("att: %S", (TCHAR*)(pKeyInfo + pKeyInfo->DataOffset));
				// The driver is done with the pKeyInfo.
				wchar_t* Names = (wchar_t*)ExAllocatePool(NonPagedPool, pKeyInfo->DataLength);
				if (NULL == Names)
				{
					FltUnregisterFilter(FilterHandle);
					return status;
				}
				DbgPrint("Data from Registry, data length: %lu \n", pKeyInfo->DataLength);
				int i = 0;
				int j = 0;
				while( i < pKeyInfo->DataLength) {
					DbgPrint("Data from Registry: %d \n", pKeyInfo->Data[i]);
					if (pKeyInfo->Data[i] != 0) {
						*(Names+j) = (wchar_t)(pKeyInfo->Data[i]);
						DbgPrint("casted to wchar_t: %C \n", *(Names+j));
						j++;
					}
					else {
						if (i < pKeyInfo->DataLength - 1)
						{
							if (pKeyInfo->Data[i] == pKeyInfo->Data[i + 1]) {
								DbgPrint("inserting null terminator kinda \n");
								*(Names + j) = L'\0';
								j++;
								i++;
							}
						}
					}
					i++;
				}
				FolderName = Names;
				ExFreePool(pKeyInfo);
				DbgPrint("Folder Name: %S \n", FolderName);

				if (NULL != handleRegKey)
				{
					ZwClose(handleRegKey);
				}
			}
		}
	}
	lock = 2;
	return status;
}