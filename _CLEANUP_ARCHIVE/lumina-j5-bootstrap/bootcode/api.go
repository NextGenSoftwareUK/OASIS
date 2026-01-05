package main

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"strconv"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"gorm.io/gorm"
)

// API Handlers for HTTP endpoints

// createAttestation creates a new attestation
func (b *LuminaJ5Bootstrap) createAttestation(c *gin.Context) {
	var attestation Attestation
	if err := c.ShouldBindJSON(&attestation); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Generate ID if not provided
	if attestation.ID == "" {
		attestation.ID = uuid.New().String()
	}

	// Set timestamps
	attestation.CreatedAt = time.Now()
	attestation.Status = "pending"

	// Save to database
	if err := b.DB.Create(&attestation).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create attestation"})
		return
	}

	// Publish event
	eventData := map[string]interface{}{
		"id":        attestation.ID,
		"type":      attestation.Type,
		"attestor":  attestation.Attestor,
		"timestamp": attestation.CreatedAt.Unix(),
	}

	eventPayload, _ := json.Marshal(eventData)
	if err := b.NATS.Publish("gov.attestation.created", eventPayload); err != nil {
		// Log error but don't fail the request
		fmt.Printf("Failed to publish attestation event: %v\n", err)
	}

	c.JSON(http.StatusCreated, gin.H{
		"success": true,
		"result":  attestation,
	})
}

// listAttestations lists attestations with pagination
func (b *LuminaJ5Bootstrap) listAttestations(c *gin.Context) {
	page, _ := strconv.Atoi(c.DefaultQuery("page", "1"))
	limit, _ := strconv.Atoi(c.DefaultQuery("limit", "20"))
	status := c.Query("status")
	attestor := c.Query("attestor")

	offset := (page - 1) * limit

	query := b.DB.Model(&Attestation{})

	if status != "" {
		query = query.Where("status = ?", status)
	}

	if attestor != "" {
		query = query.Where("attestor = ?", attestor)
	}

	var attestations []Attestation
	var total int64

	query.Count(&total)
	if err := query.Offset(offset).Limit(limit).Order("created_at DESC").Find(&attestations).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch attestations"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"success": true,
		"result": gin.H{
			"attestations": attestations,
			"pagination": gin.H{
				"page":  page,
				"limit": limit,
				"total": total,
			},
		},
	})
}

// createPolicyDecision creates a new policy decision
func (b *LuminaJ5Bootstrap) createPolicyDecision(c *gin.Context) {
	var decision PolicyDecision
	if err := c.ShouldBindJSON(&decision); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Generate ID if not provided
	if decision.ID == "" {
		decision.ID = uuid.New().String()
	}

	// Set timestamp
	decision.CreatedAt = time.Now()

	// Save to database
	if err := b.DB.Create(&decision).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to create policy decision"})
		return
	}

	// Publish event
	eventData := map[string]interface{}{
		"id":        decision.ID,
		"policy":    decision.Policy,
		"decision":  decision.Decision,
		"timestamp": decision.CreatedAt.Unix(),
	}

	eventPayload, _ := json.Marshal(eventData)
	if err := b.NATS.Publish("gov.policy.decision", eventPayload); err != nil {
		fmt.Printf("Failed to publish policy decision event: %v\n", err)
	}

	c.JSON(http.StatusCreated, gin.H{
		"success": true,
		"result":  decision,
	})
}

// listPolicyDecisions lists policy decisions with pagination
func (b *LuminaJ5Bootstrap) listPolicyDecisions(c *gin.Context) {
	page, _ := strconv.Atoi(c.DefaultQuery("page", "1"))
	limit, _ := strconv.Atoi(c.DefaultQuery("limit", "20"))
	policy := c.Query("policy")
	decision := c.Query("decision")

	offset := (page - 1) * limit

	query := b.DB.Model(&PolicyDecision{})

	if policy != "" {
		query = query.Where("policy = ?", policy)
	}

	if decision != "" {
		query = query.Where("decision = ?", decision)
	}

	var decisions []PolicyDecision
	var total int64

	query.Count(&total)
	if err := query.Offset(offset).Limit(limit).Order("created_at DESC").Find(&decisions).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch policy decisions"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"success": true,
		"result": gin.H{
			"decisions": decisions,
			"pagination": gin.H{
				"page":  page,
				"limit": limit,
				"total": total,
			},
		},
	})
}

// proposeTransaction proposes a new transaction
func (b *LuminaJ5Bootstrap) proposeTransaction(c *gin.Context) {
	var tx Transaction
	if err := c.ShouldBindJSON(&tx); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Generate ID if not provided
	if tx.ID == "" {
		tx.ID = uuid.New().String()
	}

	// Set initial status and timestamp
	tx.Status = "proposed"
	tx.CreatedAt = time.Now()

	// Save to database
	if err := b.DB.Create(&tx).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to propose transaction"})
		return
	}

	// Publish event
	eventData := map[string]interface{}{
		"id":         tx.ID,
		"type":       tx.Type,
		"proposed_by": tx.ProposedBy,
		"timestamp":  tx.CreatedAt.Unix(),
	}

	eventPayload, _ := json.Marshal(eventData)
	if err := b.NATS.Publish("exec.tx.proposed", eventPayload); err != nil {
		fmt.Printf("Failed to publish transaction proposed event: %v\n", err)
	}

	c.JSON(http.StatusCreated, gin.H{
		"success": true,
		"result":  tx,
	})
}

// listTransactions lists transactions with pagination
func (b *LuminaJ5Bootstrap) listTransactions(c *gin.Context) {
	page, _ := strconv.Atoi(c.DefaultQuery("page", "1"))
	limit, _ := strconv.Atoi(c.DefaultQuery("limit", "20"))
	status := c.Query("status")
	proposedBy := c.Query("proposed_by")

	offset := (page - 1) * limit

	query := b.DB.Model(&Transaction{})

	if status != "" {
		query = query.Where("status = ?", status)
	}

	if proposedBy != "" {
		query = query.Where("proposed_by = ?", proposedBy)
	}

	var transactions []Transaction
	var total int64

	query.Count(&total)
	if err := query.Offset(offset).Limit(limit).Order("created_at DESC").Find(&transactions).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch transactions"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"success": true,
		"result": gin.H{
			"transactions": transactions,
			"pagination": gin.H{
				"page":  page,
				"limit": limit,
				"total": total,
			},
		},
	})
}

// approveTransaction approves a transaction
func (b *LuminaJ5Bootstrap) approveTransaction(c *gin.Context) {
	transactionID := c.Param("id")
	approver := c.GetHeader("X-User-ID")

	if approver == "" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "X-User-ID header required"})
		return
	}

	// Get transaction
	var tx Transaction
	if err := b.DB.First(&tx, "id = ?", transactionID).Error; err != nil {
		if err == gorm.ErrRecordNotFound {
			c.JSON(http.StatusNotFound, gin.H{"error": "Transaction not found"})
			return
		}
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch transaction"})
		return
	}

	// Check if already approved
	if tx.Status != "proposed" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Transaction is not in proposed status"})
		return
	}

	// Add approver to approved_by list
	approvedBy := tx.ApprovedBy
	if approvedBy == nil {
		approvedBy = []string{}
	}

	// Check if already approved by this user
	for _, existingApprover := range approvedBy {
		if existingApprover == approver {
			c.JSON(http.StatusBadRequest, gin.H{"error": "Transaction already approved by this user"})
			return
		}
	}

	approvedBy = append(approvedBy, approver)

	// Check if quorum is met
	quorumMet := len(approvedBy) >= int(float64(len(approvedBy)+1) * b.Config.Governance.Quorum.Min)

	// Update transaction
	updateData := map[string]interface{}{
		"approved_by": approvedBy,
	}

	if quorumMet {
		updateData["status"] = "approved"
	}

	if err := b.DB.Model(&tx).Updates(updateData).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to update transaction"})
		return
	}

	c.JSON(http.StatusOK, gin.H{
		"success":     true,
		"quorum_met": quorumMet,
		"approved_by": approvedBy,
		"message":     "Transaction approved successfully",
	})
}

// executeTransaction executes a transaction
func (b *LuminaJ5Bootstrap) executeTransaction(c *gin.Context) {
	transactionID := c.Param("id")
	executor := c.GetHeader("X-User-ID")

	if executor == "" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "X-User-ID header required"})
		return
	}

	// Get transaction
	var tx Transaction
	if err := b.DB.First(&tx, "id = ?", transactionID).Error; err != nil {
		if err == gorm.ErrRecordNotFound {
			c.JSON(http.StatusNotFound, gin.H{"error": "Transaction not found"})
			return
		}
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch transaction"})
		return
	}

	// Check if transaction is approved
	if tx.Status != "approved" {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Transaction must be approved before execution"})
		return
	}

	// Execute transaction using the execute handler
	executeHandler := b.Services["execute"].(*ExecuteHandler)
	
	payload := map[string]interface{}{
		"transaction_id": tx.ID,
		"action":         tx.Type,
		"payload":        tx.Payload,
		"executor":       executor,
	}

	payloadBytes, _ := json.Marshal(payload)
	result, err := executeHandler.Handle(context.Background(), payloadBytes)

	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": fmt.Sprintf("Execution failed: %v", err)})
		return
	}

	var executionResult map[string]interface{}
	json.Unmarshal(result, &executionResult)

	c.JSON(http.StatusOK, gin.H{
		"success": true,
		"result":  executionResult,
	})
}

// getHerzCoherence gets Herz coherence metrics
func (b *LuminaJ5Bootstrap) getHerzCoherence(c *gin.Context) {
	window := c.DefaultQuery("window", "900") // Default 15 minutes
	windowSeconds, _ := strconv.Atoi(window)

	// Get Herz coherence data for the specified window
	var coherence []HerzCoherence
	query := b.DB.Model(&HerzCoherence{})

	if windowSeconds > 0 {
		since := time.Now().Add(-time.Duration(windowSeconds) * time.Second)
		query = query.Where("timestamp >= ?", since)
	}

	if err := query.Order("timestamp DESC").Find(&coherence).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch Herz coherence data"})
		return
	}

	// Calculate current coherence
	currentCoherence := 0.0
	if len(coherence) > 0 {
		currentCoherence = coherence[0].Value
	}

	// Calculate average coherence
	avgCoherence := 0.0
	if len(coherence) > 0 {
		total := 0.0
		for _, c := range coherence {
			total += c.Value
		}
		avgCoherence = total / float64(len(coherence))
	}

	c.JSON(http.StatusOK, gin.H{
		"success": true,
		"result": gin.H{
			"current":     currentCoherence,
			"average":     avgCoherence,
			"threshold":   b.Config.Governance.HerzCoherence.Min,
			"window":      windowSeconds,
			"data_points": len(coherence),
			"history":     coherence,
		},
	})
}

// updateHerzCoherence updates Herz coherence metrics
func (b *LuminaJ5Bootstrap) updateHerzCoherence(c *gin.Context) {
	var request struct {
		Value  float64 `json:"value" binding:"required"`
		Window int     `json:"window"`
	}

	if err := c.ShouldBindJSON(&request); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	// Create new Herz coherence record
	coherence := HerzCoherence{
		ID:        uuid.New().String(),
		Timestamp: time.Now(),
		Value:     request.Value,
		Window:    request.Window,
		Status:    "above_threshold",
	}

	if request.Value < b.Config.Governance.HerzCoherence.Min {
		coherence.Status = "below_threshold"
	}

	// Save to database
	if err := b.DB.Create(&coherence).Error; err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to update Herz coherence"})
		return
	}

	c.JSON(http.StatusCreated, gin.H{
		"success": true,
		"result":  coherence,
	})
}

// handleService handles service requests
func (b *LuminaJ5Bootstrap) handleService(c *gin.Context) {
	serviceName := c.Param("name")
	
	// Get service handler
	handler, exists := b.Services[serviceName]
	if !exists {
		c.JSON(http.StatusNotFound, gin.H{"error": "Service not found"})
		return
	}

	// Read request body
	body, err := c.GetRawData()
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Failed to read request body"})
		return
	}

	// Handle the request
	result, err := handler.Handle(context.Background(), body)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	// Return result
	c.Data(http.StatusOK, "application/json", result)
}

// Event handlers for NATS events

// handleAttestationCreated handles attestation created events
func (b *LuminaJ5Bootstrap) handleAttestationCreated(data []byte) {
	var event map[string]interface{}
	if err := json.Unmarshal(data, &event); err != nil {
		fmt.Printf("Failed to unmarshal attestation event: %v\n", err)
		return
	}

	fmt.Printf("Attestation created: %s by %s\n", event["id"], event["attestor"])

	// Perform any additional processing for attestation creation
	// For example, trigger compliance checks, update governance state, etc.
}

// handlePolicyDecision handles policy decision events
func (b *LuminaJ5Bootstrap) handlePolicyDecision(data []byte) {
	var event map[string]interface{}
	if err := json.Unmarshal(data, &event); err != nil {
		fmt.Printf("Failed to unmarshal policy decision event: %v\n", err)
		return
	}

	fmt.Printf("Policy decision: %s for policy %s\n", event["decision"], event["policy"])

	// Perform any additional processing for policy decisions
	// For example, trigger truth anchoring, update governance state, etc.
}

// handleTransactionProposed handles transaction proposed events
func (b *LuminaJ5Bootstrap) handleTransactionProposed(data []byte) {
	var event map[string]interface{}
	if err := json.Unmarshal(data, &event); err != nil {
		fmt.Printf("Failed to unmarshal transaction proposed event: %v\n", err)
		return
	}

	fmt.Printf("Transaction proposed: %s by %s\n", event["id"], event["proposed_by"])

	// Perform any additional processing for proposed transactions
	// For example, trigger compliance checks, notify approvers, etc.
}

// handleTransactionCommitted handles transaction committed events
func (b *LuminaJ5Bootstrap) handleTransactionCommitted(data []byte) {
	var event map[string]interface{}
	if err := json.Unmarshal(data, &event); err != nil {
		fmt.Printf("Failed to unmarshal transaction committed event: %v\n", err)
		return
	}

	fmt.Printf("Transaction committed: %s with status %s\n", event["transaction_id"], event["status"])

	// Perform any additional processing for committed transactions
	// For example, trigger truth anchoring, update state, etc.
}

