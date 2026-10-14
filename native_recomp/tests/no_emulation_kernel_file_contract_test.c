#include "recomp_types.h"
#include "kernel.h"

#include <windows.h>

#include <stdio.h>

/* The file contract is a host-service test.  Keep every guest callback
 * unresolved so this executable cannot enter generated game code. */
recomp_func_t recomp_lookup(uint32_t xbox_va)
{
    (void)xbox_va;
    return NULL;
}

static int fail(const char *message)
{
    fprintf(stderr, "kernel_file_contract: %s\n", message);
    return 1;
}

int main(void)
{
    const char *path = "fut-street-2-kernel-file-contract-test.bin";
    HANDLE file;
    XBOX_IO_STATUS_BLOCK io = {0};
    XBOX_FILE_POSITION_INFORMATION position = {0};
    XBOX_FILE_NETWORK_OPEN_INFORMATION network = {0};
    XBOX_FILE_END_OF_FILE_INFORMATION end_of_file = {0};
    XBOX_FILE_STANDARD_INFORMATION standard = {0};
    NTSTATUS status;

    DeleteFileA(path);
    file = CreateFileA(path, GENERIC_READ | GENERIC_WRITE, 0, NULL,
                       CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (file == INVALID_HANDLE_VALUE)
        return fail("CreateFileA failed");

    status = xbox_NtQueryInformationFile(
        file, &io, &position, sizeof(position), XboxFilePositionInformation);
    if (status != STATUS_SUCCESS || io.Status != STATUS_SUCCESS ||
        io.Information != sizeof(position) || position.CurrentByteOffset.QuadPart != 0)
        return fail("XboxFilePositionInformation query failed");

    position.CurrentByteOffset.QuadPart = 3;
    status = xbox_NtSetInformationFile(
        file, &io, &position, sizeof(position), XboxFilePositionInformation);
    if (status != STATUS_SUCCESS || io.Status != STATUS_SUCCESS)
        return fail("XboxFilePositionInformation set failed");

    position.CurrentByteOffset.QuadPart = 0;
    status = xbox_NtQueryInformationFile(
        file, &io, &position, sizeof(position), XboxFilePositionInformation);
    if (status != STATUS_SUCCESS || position.CurrentByteOffset.QuadPart != 3)
        return fail("file position did not round-trip");

    status = xbox_NtQueryInformationFile(
        file, &io, &network, sizeof(network), XboxFileNetworkOpenInformation);
    if (status != STATUS_SUCCESS || io.Status != STATUS_SUCCESS ||
        io.Information != sizeof(network) || network.EndOfFile.QuadPart != 0)
        return fail("XboxFileNetworkOpenInformation query failed");

    end_of_file.EndOfFile.QuadPart = 4096;
    status = xbox_NtSetInformationFile(
        file, &io, &end_of_file, sizeof(end_of_file),
        XboxFileAllocationInformation);
    if (status != STATUS_SUCCESS || io.Status != STATUS_SUCCESS)
        return fail("XboxFileAllocationInformation set failed");

    status = xbox_NtSetInformationFile(
        file, &io, &end_of_file, sizeof(end_of_file),
        XboxFileEndOfFileInformation);
    if (status != STATUS_SUCCESS || io.Status != STATUS_SUCCESS)
        return fail("XboxFileEndOfFileInformation set failed");

    status = xbox_NtQueryInformationFile(
        file, &io, &standard, sizeof(standard), XboxFileStandardInformation);
    if (status != STATUS_SUCCESS || standard.EndOfFile.QuadPart != 4096)
        return fail("allocated file size did not round-trip");

    if (xbox_RtlNtStatusToDosError(STATUS_SUCCESS) != ERROR_SUCCESS ||
        xbox_RtlNtStatusToDosError(STATUS_NOT_IMPLEMENTED) !=
            ERROR_CALL_NOT_IMPLEMENTED ||
        xbox_RtlNtStatusToDosError((NTSTATUS)0x12345678L) !=
            ERROR_MR_MID_NOT_FOUND)
        return fail("NTSTATUS to DOS error mapping failed");

    CloseHandle(file);
    DeleteFileA(path);
    puts("kernel_file_contract: position/network-open/allocation/end-of-file/status-map passed");
    return 0;
}
