# Subscription cancellation refunds

Annual cancellation ends access at the next monthly boundary anchored to the subscription start. Remaining full months are refunded from the successful annual charge's actual gross amount, rounded to two decimals. Monthly cancellation retains the paid monthly period and does not create an ordinary cancellation refund.

Cancellation records the original end date, shortened end, remaining months, payment and proposed refund amount. Missing, ambiguous, already-refunded or prorated upgrade charges require review; no guessed amount can be approved.

## Admin workflow

Open **Yönetim > Ödeme inceleme > Abonelik iptal iadeleri**. The queue is available only to Systemcel administrators. It shows up to 100 recent annual cancellation records, including unresolved and completed refunds.

1. Check the business, cancellation date, access end and fixed refund amount.
2. Choose **Onayla**. The server recalculates the stored proposal at its cancellation timestamp, checks the payment and competing refunds, and atomically records the first approving account/time and a single refund instruction. Approval does not call PayTR.
3. Choose **Gönder**, then confirm **Test iadesini gönder**. Only a configured test refund provider can execute this action. Preflight must match the persisted charge, test mode and previously confirmed refunds. Otherwise the instruction requires review without sending money.
4. Use **Durumu sorgula** for an unresolved result. An accepted request is not a confirmed refund. Confirmation requires the exact provider reference and amount; unresolved instructions cannot be sent again. The existing reconciliation job also checks unresolved instructions.

The customer's subscription page displays the persisted review, approval, pending result, completion or failure state. A refund for an older charge does not change a newer paid period.

## Deployment and limits

Apply `SubscriptionCancellationPolicy` and `CancellationRefundApproval` PostgreSQL migrations in order. Both are additive; they do not recalculate old orders, commissions, fees or payouts. The SQLite compatibility migrator adds the corresponding fields and refund instruction table for existing local databases.

New marketplace orders use 9% of the product subtotal before VAT. Commission VAT and withholding rules are unchanged. Configured payment service cost is stored separately as Systemcel's expense and no longer reduces the supplier payout. Existing order snapshots and their historical fee deductions remain intact.

Production must retain `Unconfigured` payment mode until the merchant and acceptance gates are resolved. This workflow does not enable live payments, automatic renewals or supplier transfers. Actual PayTR refund confirmation, merchant permissions and accounting/document approval remain separate acceptance work. Records marked for review need investigation; this screen does not allow arbitrary refund amount overrides or retries of uncertain requests.
