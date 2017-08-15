﻿using CGM.Communication.Extensions;
using CGM.Communication.Log;
using CGM.Communication.MiniMed.Responses;
using CGM.Communication.MiniMed.Responses.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using CGM.Communication.Extensions;
using System.IO;

namespace CGM.Communication.Common.Serialize
{
    public class SegementHandler
    {
        private ILogger Logger = ApplicationLogging.CreateLogger<SegementHandler>();

        public InitiateMultiPacketTransferResponse Init { get; set; }
        public PumpStateHistoryStart HistoryStart { get; set; }
        public List<PumpEvent> Events { get; set; } = new List<PumpEvent>();
        public List<PumpStateHistory> PumpStateHistory { get; set; } = new List<PumpStateHistory>();
        public int ReadLength { get; set; } = 0;

        private MultiPacketHandler _handler;


        public SegementHandler(MultiPacketHandler handler, InitiateMultiPacketTransferResponse response)
        {
            _handler = handler;
            Init = response;
        }

        public void AddHistory(PumpStateHistory history)
        {
            PumpStateHistory.Add(history);
            ReadLength += history.Message.Length;
        }

        public void GetHistoryEvents()
        {

            if (this.PumpStateHistory.Count==0)
            {
                Logger.LogError($"No history read: {this._handler.ReadInfoResponse.HistoryDataType.ToString()}");
                return;
            }

            if (this.PumpStateHistory.Count!=this.Init.PacketsToFetch)
            {
                Logger.LogError($"Wrong number of PacketsToFetch  {this._handler.ReadInfoResponse.HistoryDataType.ToString()} (expected {this.Init.PacketsToFetch}/{this.PumpStateHistory.Count})");
            }

            int block_size = 2048;
            byte[] blockpayload;
            Events = new List<PumpEvent>();
            List<byte> segmentbytes = new List<byte>();

            var all = PumpStateHistory.Select(e => e.Message);

            foreach (var item in all)
            {
                segmentbytes.AddRange(item);
            }

            HistoryStart = _handler._seri.Deserialize<PumpStateHistoryStart>(segmentbytes.ToArray());
            if (this.Init.SegmentSize != this.ReadLength)
            {
                Logger.LogError($"Wrong segmentsize in {this._handler.ReadInfoResponse.HistoryDataType.ToString()} ({this.Init.SegmentSize}/{this.ReadLength})");
                return;
            }

            //int remainderBefore = (int)Math.IEEERemainder(HistoryStart.historySizeUncompressed, block_size);
            //int lzoSize = HistoryStart.historySizeUncompressed + remainderBefore;
            if (HistoryStart.historyCompressed == 0x01)
            {
                using (Stream stream = new MemoryStream(HistoryStart.AllBytesNoHeader))
                using (var ms = new MemoryStream())
                using (var decompressed1 = new LzoStream(stream, CompressionMode.Decompress, false, HistoryStart.historySizeUncompressed))
                {
                    blockpayload = decompressed1.ToByteArray();
                }
            }
            else
            {
                blockpayload = HistoryStart.AllBytesNoHeader;
            }
            int remainder = (int)Math.IEEERemainder(blockpayload.Length, block_size);

           

            //ignorering this and check the checksum, then we get some of the events...... but is it save to do this?
            if ((blockpayload.Length % block_size) != 0)
            {
                Logger.LogError($"{this._handler.ReadInfoResponse.HistoryDataType.ToString()}: Block payload size is not a multiple of 2048 ({blockpayload.Length.ToString()} -> {remainder.ToString()})");
               return;
            }

            var length = blockpayload.Length / block_size;
            for (int i = 0; i < length; i++)
            {
                var blockStart = i * block_size;
                var blockSize = blockpayload.GetUInt16BigE(((i + 1) * block_size) - 4);
                var blockChecksum = blockpayload.GetUInt16BigE(((i + 1) * block_size) - 2);
                if ((blockStart + blockSize) <= blockpayload.Length)
                {
                    var blockData = blockpayload.ToList().GetRange(blockStart, blockSize);
                    var calculatedChecksum2 = Crc16Ccitt.CRC16CCITT(blockData.ToArray(), 0xFFFF, 0x1021, blockSize);

                    if (blockChecksum == calculatedChecksum2)
                    {
                        GetEvents(blockData, 0);
                    }
                    else
                    {
                        Logger.LogError($"CRC16CCITT does not match.");
                        return;
                    }
                }

            }
        }

        private void GetEvents(List<byte> bytes, int start)
        {
            var length = bytes[start + 2];
            var bytesMessage = bytes.GetRange(start, length).ToArray();
            var eventmessage = _handler._seri.Deserialize<PumpEvent>(bytesMessage);

            Events.Add(eventmessage);

            int newstart = start + length;
            if (newstart < bytes.Count)
            {
                GetEvents(bytes, newstart);
            }

        }

    }
}
